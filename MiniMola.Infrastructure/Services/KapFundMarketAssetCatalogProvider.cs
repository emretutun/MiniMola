using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class KapFundMarketAssetCatalogProvider(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<KapFundMarketAssetCatalogProvider> logger)
    : IMarketAssetCatalogProvider
{
    private const string QuoteCurrency = "TRY";

    private static readonly TimeSpan SyncInterval =
        TimeSpan.FromHours(20);

    private static readonly SemaphoreSlim SyncLock =
        new(1, 1);

    private static readonly FundCatalog[] Catalogs =
    [
        new(
            "YF",
            MarketAssetType.InvestmentFund,
            "TEFAS",
            "TEFAS_YAT",
            1_500),

        new(
            "EYF",
            MarketAssetType.PensionFund,
            "BEFAS",
            "TEFAS_EMK",
            200),

        new(
            "BYF",
            MarketAssetType.ExchangeTradedFund,
            "BIST",
            "TEFAS_BYF",
            20)
    ];

    private static readonly Regex FundRowPattern =
        new(
            "<a\\s+href=\"/tr/fon-bilgileri/ozet/" +
            "[^\"]+\"[^>]*>\\s*<span[^>]*>" +
            "(?<code>[A-Z0-9]{2,8})</span>\\s*</a>" +
            "\\s*</td>\\s*<td[^>]*>\\s*" +
            "<a\\s+href=\"/tr/fon-bilgileri/ozet/" +
            "[^\"]+\"[^>]*>(?<name>[^<]+)</a>",
            RegexOptions.IgnoreCase
            | RegexOptions.Singleline
            | RegexOptions.Compiled);

    public async Task SyncAsync(
        CancellationToken cancellationToken = default)
    {
        await SyncLock.WaitAsync(cancellationToken);

        try
        {
            foreach (var catalog in Catalogs)
            {
                await SyncCatalogAsync(
                    catalog,
                    cancellationToken);
            }
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task SyncCatalogAsync(
        FundCatalog catalog,
        CancellationToken cancellationToken)
    {
        try
        {
            if (await IsCatalogFreshAsync(
                    catalog,
                    cancellationToken))
            {
                return;
            }

            var funds =
                await GetFundsAsync(
                    catalog.KapCategory,
                    cancellationToken);

            if (funds.Count < catalog.ExpectedSize)
            {
                logger.LogWarning(
                    "KAP {Category} fon listesi beklenenden az " +
                    "kayıt döndürdü: {FundCount}.",
                    catalog.KapCategory,
                    funds.Count);

                return;
            }

            var existingAssets =
                await dbContext.MarketAssets
                    .Where(asset =>
                        asset.AssetType == catalog.AssetType
                        && asset.DataProviderCode ==
                            catalog.ProviderCode
                        && asset.QuoteCurrency == QuoteCurrency
                        && asset.ProviderSymbol != null)
                    .ToListAsync(cancellationToken);

            var existingAssetLookup =
                existingAssets.ToDictionary(
                    asset => asset.ProviderSymbol!,
                    StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var fund in funds)
            {
                if (existingAssetLookup.TryGetValue(
                        fund.Code,
                        out var existingAsset))
                {
                    existingAsset.Symbol = fund.Code;
                    existingAsset.Name = fund.Name;
                    existingAsset.MarketCode =
                        catalog.MarketCode;
                    existingAsset.IsActive = true;
                    existingAsset.UpdatedAtUtc = now;
                    updatedCount++;
                    continue;
                }

                var newAsset =
                    new MarketAsset
                    {
                        Symbol = fund.Code,
                        Name = fund.Name,
                        AssetType = catalog.AssetType,
                        MarketCode = catalog.MarketCode,
                        QuoteCurrency = QuoteCurrency,
                        DataProviderCode =
                            catalog.ProviderCode,
                        ProviderSymbol = fund.Code,
                        IsFeatured = false,
                        IsActive = true,
                        CreatedAtUtc = now
                    };

                dbContext.MarketAssets.Add(newAsset);
                existingAssetLookup.Add(
                    fund.Code,
                    newAsset);
                addedCount++;
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            logger.LogInformation(
                "KAP {Category} fon kataloğu senkronize edildi. " +
                "Eklenen: {AddedCount}, güncellenen: {UpdatedCount}.",
                catalog.KapCategory,
                addedCount,
                updatedCount);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "KAP {Category} fon listesine ulaşılamadı.",
                catalog.KapCategory);
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "KAP {Category} fon listesi isteği " +
                "zaman aşımına uğradı.",
                catalog.KapCategory);
        }
    }

    private async Task<bool> IsCatalogFreshAsync(
        FundCatalog catalog,
        CancellationToken cancellationToken)
    {
        var catalogQuery =
            dbContext.MarketAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.AssetType == catalog.AssetType
                    && asset.DataProviderCode ==
                        catalog.ProviderCode
                    && asset.IsActive);

        var assetCount =
            await catalogQuery.CountAsync(
                cancellationToken);

        if (assetCount < catalog.ExpectedSize)
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

    private async Task<List<KapFund>> GetFundsAsync(
        string kapCategory,
        CancellationToken cancellationToken)
    {
        using var response =
            await httpClient.GetAsync(
                $"tr/YatirimFonlari/{kapCategory}",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "KAP {Category} fon listesi isteği " +
                "{StatusCode} koduyla başarısız oldu.",
                kapCategory,
                (int)response.StatusCode);

            return [];
        }

        var html =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        var funds =
            new List<KapFund>();

        foreach (Match match in
            FundRowPattern.Matches(html))
        {
            var code =
                match.Groups["code"].Value
                    .Trim()
                    .ToUpperInvariant();

            var name = NormalizeName(
                WebUtility.HtmlDecode(
                    match.Groups["name"].Value));

            if (!string.IsNullOrWhiteSpace(code)
                && !string.IsNullOrWhiteSpace(name))
            {
                funds.Add(new KapFund(code, name));
            }
        }

        return funds
            .DistinctBy(
                fund => fund.Code,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(fund => fund.Code)
            .ToList();
    }

    private static string NormalizeName(string name)
    {
        var normalized =
            Regex.Replace(
                    name,
                    "\\s+",
                    " ")
                .Trim();

        return normalized.Length <= 200
            ? normalized
            : normalized[..200];
    }

    private sealed record FundCatalog(
        string KapCategory,
        MarketAssetType AssetType,
        string MarketCode,
        string ProviderCode,
        int ExpectedSize);

    private sealed record KapFund(
        string Code,
        string Name);
}
