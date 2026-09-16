using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class YahooFinanceMarketPriceProvider(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<YahooFinanceMarketPriceProvider> logger)
    : IMarketPriceProvider
{
    private const string ProviderCode =
        "YAHOO_FINANCE";

    private const string SourceName =
        "Yahoo Finance";

    private static readonly TimeSpan RequestInterval =
        TimeSpan.FromMinutes(5);

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
                        && asset.ProviderSymbol != null)
                    .ToListAsync(cancellationToken);

            if (assets.Count == 0)
            {
                return;
            }

            var assetIds =
                assets
                    .Select(asset => asset.Id)
                    .ToList();

            var latestSnapshots =
                await dbContext.MarketPriceSnapshots
                    .AsNoTracking()
                    .Where(snapshot =>
                        assetIds.Contains(
                            snapshot.MarketAssetId)
                        && snapshot.Source == SourceName)
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
                DateTime.UtcNow - RequestInterval;

            var staleAssets =
                assets
                    .Where(asset =>
                        !latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        || (latestSnapshot.UpdatedAtUtc ?? latestSnapshot.CreatedAtUtc)
                            < freshnessLimit)
                    .ToList();

            if (staleAssets.Count == 0)
            {
                return;
            }

            var newSnapshots =
                new List<MarketPriceSnapshot>();

            foreach (var batch in staleAssets.Chunk(5))
            {
                var quoteResults =
                    await Task.WhenAll(
                        batch.Select(async asset => new
                        {
                            Asset = asset,
                            Quote = await GetQuoteAsync(
                                asset.ProviderSymbol!,
                                cancellationToken)
                        }));

                foreach (var result in quoteResults)
                {
                    var asset = result.Asset;
                    var quote = result.Quote;

                    if (quote is null)
                    {
                        continue;
                    }

                    if (latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        && latestSnapshot.ObservedAtUtc >= quote.ObservedAtUtc)
                    {
                        if (latestSnapshot.ObservedAtUtc == quote.ObservedAtUtc)
                        {
                            await dbContext.MarketPriceSnapshots
                                .Where(x => x.Id == latestSnapshot.Id)
                                .ExecuteUpdateAsync(update => update
                                    .SetProperty(x => x.Price, quote.Price)
                                    .SetProperty(x => x.DailyChangePercent, quote.DailyChangePercent)
                                    .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow), cancellationToken);
                        }
                        continue;
                    }

                    newSnapshots.Add(
                        new MarketPriceSnapshot
                        {
                            MarketAssetId = asset.Id,
                            Price = quote.Price,
                            DailyChangePercent =
                                quote.DailyChangePercent,
                            PriceKind =
                                MarketPriceKind.Delayed,
                            Source = SourceName,
                            ObservedAtUtc =
                                quote.ObservedAtUtc
                        });
                }
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
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<YahooQuote?> GetQuoteAsync(
        string providerSymbol,
        CancellationToken cancellationToken)
    {
        try
        {
            var encodedSymbol =
                Uri.EscapeDataString(
                    providerSymbol);

            var requestUri =
                $"v8/finance/chart/{encodedSymbol}" +
                "?interval=1d&range=5d";

            using var response =
                await httpClient.GetAsync(
                    requestUri,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Yahoo Finance {Symbol} isteği " +
                    "{StatusCode} koduyla başarısız oldu.",
                    providerSymbol,
                    (int)response.StatusCode);

                return null;
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

            if (!document.RootElement.TryGetProperty(
                    "chart",
                    out var chart)
                || !chart.TryGetProperty(
                    "result",
                    out var results)
                || results.ValueKind
                    != JsonValueKind.Array
                || results.GetArrayLength() == 0
                || !results[0].TryGetProperty(
                    "meta",
                    out var metadata))
            {
                logger.LogWarning(
                    "Yahoo Finance {Symbol} için " +
                    "beklenen cevabı döndürmedi.",
                    providerSymbol);

                return null;
            }

            if (!metadata.TryGetProperty(
                    "regularMarketPrice",
                    out var priceElement)
                || !priceElement.TryGetDecimal(
                    out var price)
                || price <= 0)
            {
                return null;
            }

            if (!metadata.TryGetProperty(
                    "regularMarketTime",
                    out var timeElement)
                || !timeElement.TryGetInt64(
                    out var unixSeconds)
                || unixSeconds is
                    <= 0 or > 253402300799)
            {
                return null;
            }

            var dailyChangePercent = YahooDailyChange.Calculate(results[0], price, unixSeconds);

            var observedAtUtc =
                DateTimeOffset
                    .FromUnixTimeSeconds(
                        unixSeconds)
                    .UtcDateTime;

            return new YahooQuote(
                price,
                dailyChangePercent,
                observedAtUtc);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Yahoo Finance {Symbol} " +
                "servisine ulaşılamadı.",
                providerSymbol);

            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Yahoo Finance {Symbol} için " +
                "geçersiz JSON döndürdü.",
                providerSymbol);

            return null;
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken
                .IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Yahoo Finance {Symbol} isteği " +
                "zaman aşımına uğradı.",
                providerSymbol);

            return null;
        }
    }

    private sealed record YahooQuote(
        decimal Price,
        decimal? DailyChangePercent,
        DateTime ObservedAtUtc);
}
