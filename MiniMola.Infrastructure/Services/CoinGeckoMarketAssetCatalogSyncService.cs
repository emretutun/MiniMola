using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class CoinGeckoMarketAssetCatalogSyncService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<CoinGeckoMarketAssetCatalogSyncService> logger)
    : IMarketAssetCatalogProvider
{
    private const string ProviderCode = "COINGECKO";
    private const string MarketCode = "CRYPTO";
    private const string QuoteCurrency = "TRY";
    private const string SourceName = "CoinGecko";
    private const int ExpectedCatalogSize = 100;

    private static readonly TimeSpan SyncInterval =
        TimeSpan.FromHours(20);

    private static readonly SemaphoreSlim SyncLock =
        new(1, 1);

    public async Task SyncAsync(
        CancellationToken cancellationToken = default)
    {
        await SyncLock.WaitAsync(cancellationToken);

        try
        {
            if (await IsCatalogFreshAsync(cancellationToken))
            {
                return;
            }

            var coins =
                await GetTopCoinsAsync(cancellationToken);

            if (coins.Count == 0)
            {
                return;
            }

            var existingAssets =
                await dbContext.MarketAssets
                    .Where(asset =>
                        asset.DataProviderCode == ProviderCode
                        && asset.QuoteCurrency == QuoteCurrency
                        && asset.ProviderSymbol != null)
                    .ToListAsync(cancellationToken);

            var existingAssetLookup =
                existingAssets.ToDictionary(
                    asset => asset.ProviderSymbol!,
                    StringComparer.OrdinalIgnoreCase);

            var synchronizedAssets =
                new List<(MarketAsset Asset, CoinGeckoMarketCoin Coin)>();

            var now = DateTime.UtcNow;
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var coin in coins
                .Where(IsValidCoin)
                .DistinctBy(
                    coin => coin.Id,
                    StringComparer.OrdinalIgnoreCase))
            {
                if (existingAssetLookup.TryGetValue(
                    coin.Id,
                    out var existingAsset))
                {
                    if (existingAsset.AssetType !=
                        MarketAssetType.CryptoCurrency)
                    {
                        continue;
                    }

                    existingAsset.Symbol =
                        NormalizeSymbol(coin.Symbol);

                    existingAsset.Name =
                        NormalizeName(coin.Name);

                    existingAsset.MarketCode = MarketCode;
                    existingAsset.IsActive = true;
                    existingAsset.UpdatedAtUtc = now;

                    synchronizedAssets.Add(
                        (existingAsset, coin));

                    updatedCount++;
                    continue;
                }

                var newAsset =
                    new MarketAsset
                    {
                        Symbol = NormalizeSymbol(
                            coin.Symbol),
                        Name = NormalizeName(coin.Name),
                        AssetType =
                            MarketAssetType.CryptoCurrency,
                        MarketCode = MarketCode,
                        QuoteCurrency = QuoteCurrency,
                        DataProviderCode = ProviderCode,
                        ProviderSymbol = coin.Id.Trim(),
                        IsFeatured = false,
                        IsActive = true,
                        CreatedAtUtc = now
                    };

                dbContext.MarketAssets.Add(newAsset);
                existingAssetLookup.Add(coin.Id, newAsset);
                synchronizedAssets.Add((newAsset, coin));
                addedCount++;
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            var snapshotCount =
                await AddPriceSnapshotsAsync(
                    synchronizedAssets,
                    now,
                    cancellationToken);

            logger.LogInformation(
                "CoinGecko kripto kataloğu senkronize edildi. " +
                "Eklenen: {AddedCount}, güncellenen: {UpdatedCount}, " +
                "fiyat görüntüsü: {SnapshotCount}.",
                addedCount,
                updatedCount,
                snapshotCount);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "CoinGecko kripto kataloğuna ulaşılamadı.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "CoinGecko kripto kataloğu geçersiz JSON döndürdü.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "CoinGecko kripto kataloğu isteği zaman aşımına uğradı.");
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<bool> IsCatalogFreshAsync(
        CancellationToken cancellationToken)
    {
        var catalogQuery =
            dbContext.MarketAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.AssetType ==
                        MarketAssetType.CryptoCurrency
                    && asset.DataProviderCode == ProviderCode
                    && asset.QuoteCurrency == QuoteCurrency
                    && asset.IsActive);

        var assetCount =
            await catalogQuery.CountAsync(
                cancellationToken);

        if (assetCount < ExpectedCatalogSize)
        {
            return false;
        }

        var lastSyncAtUtc =
            await catalogQuery.MaxAsync(
                asset =>
                    asset.UpdatedAtUtc
                    ?? asset.CreatedAtUtc,
                cancellationToken);

        return lastSyncAtUtc >=
            DateTime.UtcNow - SyncInterval;
    }

    private async Task<List<CoinGeckoMarketCoin>>
        GetTopCoinsAsync(
            CancellationToken cancellationToken)
    {
        const string requestUri =
            "coins/markets" +
            "?vs_currency=try" +
            "&order=market_cap_desc" +
            "&per_page=250" +
            "&page=1" +
            "&sparkline=false" +
            "&price_change_percentage=24h" +
            "&locale=tr" +
            "&precision=full";

        using var response =
            await httpClient.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "CoinGecko katalog isteği {StatusCode} " +
                "koduyla başarısız oldu.",
                (int)response.StatusCode);

            return [];
        }

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        return await JsonSerializer.DeserializeAsync<
            List<CoinGeckoMarketCoin>>(
                responseStream,
                cancellationToken: cancellationToken)
            ?? [];
    }

    private async Task<int> AddPriceSnapshotsAsync(
        IReadOnlyCollection<(
            MarketAsset Asset,
            CoinGeckoMarketCoin Coin)> synchronizedAssets,
        DateTime fallbackObservedAtUtc,
        CancellationToken cancellationToken)
    {
        var assetIds =
            synchronizedAssets
                .Select(item => item.Asset.Id)
                .ToList();

        var latestSnapshotLookup =
            await dbContext.MarketPriceSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    assetIds.Contains(snapshot.MarketAssetId))
                .GroupBy(snapshot => snapshot.MarketAssetId)
                .Select(group =>
                    group
                        .OrderByDescending(snapshot =>
                            snapshot.ObservedAtUtc)
                        .ThenByDescending(snapshot => snapshot.Id)
                        .First())
                .ToDictionaryAsync(
                    snapshot => snapshot.MarketAssetId,
                    cancellationToken);

        var snapshots =
            new List<MarketPriceSnapshot>();

        foreach (var (asset, coin) in synchronizedAssets)
        {
            if (!coin.CurrentPrice.HasValue
                || coin.CurrentPrice.Value <= 0)
            {
                continue;
            }

            var observedAtUtc =
                coin.LastUpdated?.UtcDateTime
                ?? fallbackObservedAtUtc;

            if (latestSnapshotLookup.TryGetValue(
                    asset.Id,
                    out var latestSnapshot)
                && latestSnapshot.ObservedAtUtc >=
                    observedAtUtc)
            {
                continue;
            }

            snapshots.Add(
                new MarketPriceSnapshot
                {
                    MarketAssetId = asset.Id,
                    Price = coin.CurrentPrice.Value,
                    DailyChangePercent =
                        coin.PriceChangePercentage24H,
                    PriceKind = MarketPriceKind.Delayed,
                    Source = SourceName,
                    ObservedAtUtc = observedAtUtc
                });
        }

        if (snapshots.Count == 0)
        {
            return 0;
        }

        dbContext.MarketPriceSnapshots.AddRange(snapshots);
        await dbContext.SaveChangesAsync(cancellationToken);

        return snapshots.Count;
    }

    private static bool IsValidCoin(
        CoinGeckoMarketCoin coin)
    {
        return !string.IsNullOrWhiteSpace(coin.Id)
            && coin.Id.Trim().Length <= 100
            && !string.IsNullOrWhiteSpace(coin.Symbol)
            && !string.IsNullOrWhiteSpace(coin.Name);
    }

    private static string NormalizeSymbol(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();

        return normalized.Length <= 30
            ? normalized
            : normalized[..30];
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();

        return normalized.Length <= 200
            ? normalized
            : normalized[..200];
    }

    private sealed record CoinGeckoMarketCoin(
        [property: JsonPropertyName("id")]
        string Id,

        [property: JsonPropertyName("symbol")]
        string Symbol,

        [property: JsonPropertyName("name")]
        string Name,

        [property: JsonPropertyName("current_price")]
        decimal? CurrentPrice,

        [property: JsonPropertyName(
            "price_change_percentage_24h")]
        decimal? PriceChangePercentage24H,

        [property: JsonPropertyName("last_updated")]
        DateTimeOffset? LastUpdated);
}
