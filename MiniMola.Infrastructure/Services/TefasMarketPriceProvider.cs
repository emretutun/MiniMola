using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class TefasMarketPriceProvider(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<TefasMarketPriceProvider> logger)
    : IMarketPriceProvider
{
    private const string SourceName = "TEFAS / BEFAS";

    private static readonly TimeSpan RefreshInterval =
        TimeSpan.FromHours(4);

    private static readonly SemaphoreSlim RefreshLock =
        new(1, 1);

    private static readonly IReadOnlyDictionary<string, string>
        ProviderKinds =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["TEFAS_YAT"] = "YAT",
                ["TEFAS_EMK"] = "EMK",
                ["TEFAS_BYF"] = "BYF"
            };

    public async Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default)
    {
        if (marketAssetIds.Count == 0)
        {
            return;
        }

        await RefreshLock.WaitAsync(cancellationToken);

        try
        {
            var requestedAssetIds =
                marketAssetIds.Distinct().ToList();

            var supportedProviderCodes =
                ProviderKinds.Keys.ToArray();

            var assets =
                await dbContext.MarketAssets
                    .AsNoTracking()
                    .Where(asset =>
                        requestedAssetIds.Contains(asset.Id)
                        && asset.IsActive
                        && asset.DataProviderCode != null
                        && supportedProviderCodes.Contains(
                            asset.DataProviderCode)
                        && asset.ProviderSymbol != null)
                    .ToListAsync(cancellationToken);

            if (assets.Count == 0)
            {
                return;
            }

            var assetIds =
                assets.Select(asset => asset.Id).ToList();

            var latestSnapshots =
                await dbContext.MarketPriceSnapshots
                    .Where(snapshot =>
                        assetIds.Contains(snapshot.MarketAssetId)
                        && snapshot.Source == SourceName)
                    .GroupBy(snapshot => snapshot.MarketAssetId)
                    .Select(group =>
                        group
                            .OrderByDescending(snapshot =>
                                snapshot.ObservedAtUtc)
                            .ThenByDescending(snapshot => snapshot.Id)
                            .First())
                    .ToListAsync(cancellationToken);

            var latestSnapshotLookup =
                latestSnapshots.ToDictionary(
                    snapshot => snapshot.MarketAssetId);

            var freshnessLimit =
                DateTime.UtcNow - RefreshInterval;

            var staleAssets =
                assets
                    .Where(asset =>
                        !latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        || (latestSnapshot.UpdatedAtUtc
                                ?? latestSnapshot.CreatedAtUtc)
                            < freshnessLimit)
                    .ToList();

            if (staleAssets.Count == 0)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            var hasChanges = false;

            foreach (var providerGroup in
                staleAssets.GroupBy(asset =>
                    asset.DataProviderCode!))
            {
                if (!ProviderKinds.TryGetValue(
                        providerGroup.Key,
                        out var fundKind))
                {
                    continue;
                }

                var rows =
                    await GetRecentPricesAsync(
                        fundKind,
                        cancellationToken);

                var requestedCodeLookup =
                    providerGroup.ToDictionary(
                        asset => asset.ProviderSymbol!,
                        StringComparer.OrdinalIgnoreCase);

                foreach (var priceGroup in
                    rows
                        .Where(row =>
                            requestedCodeLookup.ContainsKey(
                                row.FundCode))
                        .GroupBy(
                            row => row.FundCode,
                            StringComparer.OrdinalIgnoreCase))
                {
                    var orderedPrices =
                        priceGroup
                            .Where(row => row.Price > 0)
                            .OrderBy(row => row.Date)
                            .ToList();

                    if (orderedPrices.Count == 0)
                    {
                        continue;
                    }

                    var asset =
                        requestedCodeLookup[priceGroup.Key];

                    var latestPrice = orderedPrices[^1];
                    var previousPrice =
                        orderedPrices.Count > 1
                            ? orderedPrices[^2].Price
                            : (decimal?)null;

                    decimal? dailyChangePercent =
                        previousPrice > 0
                            ? decimal.Round(
                                ((latestPrice.Price
                                    - previousPrice.Value)
                                    / previousPrice.Value)
                                * 100m,
                                6,
                                MidpointRounding.AwayFromZero)
                            : null;

                    var observedAtUtc =
                        latestPrice.Date.ToDateTime(
                            new TimeOnly(12, 0),
                            DateTimeKind.Utc);

                    if (latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var existingSnapshot)
                        && existingSnapshot.ObservedAtUtc
                            >= observedAtUtc)
                    {
                        existingSnapshot.Price =
                            latestPrice.Price;
                        existingSnapshot.DailyChangePercent =
                            dailyChangePercent;
                        existingSnapshot.UpdatedAtUtc = nowUtc;
                        hasChanges = true;
                        continue;
                    }

                    dbContext.MarketPriceSnapshots.Add(
                        new MarketPriceSnapshot
                        {
                            MarketAssetId = asset.Id,
                            Price = latestPrice.Price,
                            DailyChangePercent =
                                dailyChangePercent,
                            PriceKind =
                                MarketPriceKind.Official,
                            Source = SourceName,
                            ObservedAtUtc = observedAtUtc,
                            CreatedAtUtc = nowUtc
                        });

                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "TEFAS fon fiyat servisine ulaşılamadı.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "TEFAS fon fiyat isteği zaman aşımına uğradı.");
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<IReadOnlyList<TefasPriceRow>>
        GetRecentPricesAsync(
            string fundKind,
            CancellationToken cancellationToken)
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-10);

        var requestBody =
            new Dictionary<string, object?>
            {
                ["fonTipi"] = fundKind,
                ["fonKodu"] = null,
                ["aramaMetni"] = null,
                ["fonTurKod"] = null,
                ["fonGrubu"] = null,
                ["sfonTurKod"] = null,
                ["fonTurAciklama"] = null,
                ["kurucuKod"] = null,
                ["basTarih"] = startDate.ToString(
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture),
                ["bitTarih"] = endDate.ToString(
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture),
                ["basSira"] = 1,
                ["bitSira"] = 100_000,
                ["dil"] = "TR",
                ["sFonTurKod"] = string.Empty,
                ["fonKod"] = string.Empty,
                ["fonGrup"] = string.Empty,
                ["fonUnvanTip"] = string.Empty
            };

        var requestJson =
            JsonSerializer.Serialize(requestBody);

        using var requestContent =
            new StringContent(
                requestJson,
                Encoding.UTF8,
                "application/json");

        using var response =
            await httpClient.PostAsync(
                "api/funds/fonGnlBlgSiraliGetir",
                requestContent,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "TEFAS {FundKind} fiyat isteği {StatusCode} " +
                "koduyla başarısız oldu.",
                fundKind,
                (int)response.StatusCode);

            return [];
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                TefasPriceResponse>(
                    cancellationToken:
                        cancellationToken);

        if (result?.ResultList is null)
        {
            return [];
        }

        return result.ResultList
            .Select(row =>
            {
                var hasDate = DateOnly.TryParseExact(
                    row.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date);

                return hasDate
                    ? new TefasPriceRow(
                        row.FundCode,
                        date,
                        row.Price)
                    : null;
            })
            .Where(row => row is not null)
            .Select(row => row!)
            .ToList();
    }

    private sealed record TefasPriceResponse(
        [property: JsonPropertyName("resultList")]
        IReadOnlyList<TefasPriceResponseRow>? ResultList);

    private sealed record TefasPriceResponseRow(
        [property: JsonPropertyName("fonKodu")]
        string FundCode,

        [property: JsonPropertyName("tarih")]
        string Date,

        [property: JsonPropertyName("fiyat")]
        decimal Price);

    private sealed record TefasPriceRow(
        string FundCode,
        DateOnly Date,
        decimal Price);
}
