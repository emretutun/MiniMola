using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class CoinGeckoMarketPriceRefreshService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<CoinGeckoMarketPriceRefreshService> logger)
    : IMarketPriceProvider
{
    private const string ProviderCode = "COINGECKO";
    private const string SourceName = "CoinGecko";

    private const decimal TroyOunceInGrams =
    31.1034768m;

    private const string DerivedGoldSource =
        "CoinGecko PAXG/Gram";

    private static readonly TimeSpan RefreshInterval =
        TimeSpan.FromMinutes(2);

    private static readonly SemaphoreSlim RefreshLock =
        new(1, 1);

    public async Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default)
    {
        if (marketAssetIds.Count == 0)
        {
            return;
        }

        await RefreshLock.WaitAsync(
            cancellationToken);

        try
        {
            var requestedAssetIds =
                marketAssetIds
                    .Distinct()
                    .ToList();

            var assets =
                await dbContext.MarketAssets
                    .AsNoTracking()
                    .Where(asset =>
                        requestedAssetIds.Contains(asset.Id)
                        && asset.IsActive
                        && asset.DataProviderCode
                            == ProviderCode
                        && asset.ProviderSymbol != null
                        && asset.QuoteCurrency == "TRY")
                    .ToListAsync(cancellationToken);

            if (assets.Count == 0)
            {
                return;
            }

            var providerAssetIds =
                assets
                    .Select(asset => asset.Id)
                    .ToList();

            var latestSnapshots =
                await dbContext.MarketPriceSnapshots
                    .AsNoTracking()
            .Where(snapshot =>
                providerAssetIds.Contains(
                    snapshot.MarketAssetId))
                    .GroupBy(snapshot =>
                        snapshot.MarketAssetId)
                    .Select(group =>
                        group
                            .OrderByDescending(snapshot =>
                                snapshot.ObservedAtUtc)
                            .ThenByDescending(snapshot =>
                                snapshot.Id)
                            .First())
                    .ToListAsync(cancellationToken);

            var latestSnapshotLookup =
                latestSnapshots.ToDictionary(
                    snapshot =>
                        snapshot.MarketAssetId);

            var freshnessLimit =
                DateTime.UtcNow - RefreshInterval;

            var staleAssets =
                assets
                    .Where(asset =>
                        !latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        || latestSnapshot.ObservedAtUtc
                            < freshnessLimit)
                    .ToList();

            if (staleAssets.Count == 0)
            {
                return;
            }

            var providerSymbols =
                staleAssets
                    .Select(asset =>
                        asset.ProviderSymbol!)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase);

            var encodedSymbols =
                string.Join(
                    ",",
                    providerSymbols.Select(
                        Uri.EscapeDataString));

            var requestUri =
                "simple/price" +
                $"?ids={encodedSymbols}" +
                "&vs_currencies=try" +
                "&include_24hr_change=true" +
                "&include_last_updated_at=true" +
                "&precision=full";

            try
            {
                using var response =
                    await httpClient.GetAsync(
                        requestUri,
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "CoinGecko fiyat isteği " +
                        "{StatusCode} koduyla başarısız oldu.",
                        (int)response.StatusCode);

                    return;
                }

                await using var responseStream =
                    await response.Content
                        .ReadAsStreamAsync(
                            cancellationToken);

                using var document =
                    await JsonDocument.ParseAsync(
                        responseStream,
                        cancellationToken:
                            cancellationToken);

                var newSnapshots =
                    new List<MarketPriceSnapshot>();

                foreach (var asset in staleAssets)
                {
                    if (!document.RootElement
                        .TryGetProperty(
                            asset.ProviderSymbol!,
                            out var quote))
                    {
                        continue;
                    }

                    if (!quote.TryGetProperty(
                            "try",
                            out var priceElement)
                        || !priceElement.TryGetDecimal(
                            out var price)
                        || price <= 0)
                    {
                        continue;
                    }

                    decimal? dailyChangePercent =
                        null;

                    if (quote.TryGetProperty(
                            "try_24h_change",
                            out var changeElement)
                        && changeElement.TryGetDecimal(
                            out var change))
                    {
                        dailyChangePercent = change;
                    }

                    var storedPrice = price;

                    var priceKind =
                        MarketPriceKind.Delayed;

                    var source =
                        SourceName;

                    if (string.Equals(
                            asset.Symbol,
                            "GRAM_ALTIN",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        storedPrice =
                            price / TroyOunceInGrams;

                        priceKind =
                            MarketPriceKind.Estimated;

                        source =
                            DerivedGoldSource;
                    }


                    var observedAtUtc =
                        GetObservedAtUtc(quote);

                    if (latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        && latestSnapshot.ObservedAtUtc
                            >= observedAtUtc)
                    {
                        continue;
                    }

                    newSnapshots.Add(
                        new MarketPriceSnapshot
                        {
                            MarketAssetId = asset.Id,
                            Price = storedPrice,
                            DailyChangePercent =
                                dailyChangePercent,
                            PriceKind = priceKind,
                            Source = source,
                            ObservedAtUtc =
                                observedAtUtc
                        });
                }

                if (newSnapshots.Count == 0)
                {
                    return;
                }

                dbContext.MarketPriceSnapshots
                    .AddRange(newSnapshots);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    exception,
                    "CoinGecko servisine ulaşılamadı.");
            }
            catch (JsonException exception)
            {
                logger.LogWarning(
                    exception,
                    "CoinGecko geçersiz JSON döndürdü.");
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "CoinGecko fiyat isteği " +
                    "zaman aşımına uğradı.");
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static DateTime GetObservedAtUtc(
        JsonElement quote)
    {
        if (quote.TryGetProperty(
                "last_updated_at",
                out var updatedAtElement)
            && updatedAtElement.TryGetInt64(
                out var unixSeconds)
            && unixSeconds is
                > 0 and <= 253402300799)
        {
            return DateTimeOffset
                .FromUnixTimeSeconds(unixSeconds)
                .UtcDateTime;
        }

        return DateTime.UtcNow;
    }
}