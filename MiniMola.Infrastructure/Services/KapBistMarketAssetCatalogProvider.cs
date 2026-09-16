using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class KapBistMarketAssetCatalogProvider(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<KapBistMarketAssetCatalogProvider> logger)
    : IMarketAssetCatalogProvider
{
    private const string DataProviderCode =
        "YAHOO_FINANCE";

    private const string MarketCode = "BIST";
    private const string QuoteCurrency = "TRY";
    private const int ExpectedCatalogSize = 500;

    private static readonly TimeSpan SyncInterval =
        TimeSpan.FromHours(20);

    private static readonly SemaphoreSlim SyncLock =
        new(1, 1);

    private static readonly Regex CompanyRowPattern =
        new(
            "<a\\s+href=\"/tr/sirket-bilgileri/ozet/" +
            "[^\"]+\">\\s*(?<codes>(?:<div>[^<]+</div>\\s*)+)" +
            "\\s*</a>\\s*</td>\\s*<td[^>]*>\\s*" +
            "<a\\s+href=\"/tr/sirket-bilgileri/ozet/" +
            "[^\"]+\">(?<name>[^<]+)</a>",
            RegexOptions.IgnoreCase
            | RegexOptions.Singleline
            | RegexOptions.Compiled);

    private static readonly Regex StockCodePattern =
        new(
            "^[A-Z0-9]{3,8}$",
            RegexOptions.CultureInvariant
            | RegexOptions.Compiled);

    public async Task SyncAsync(
        CancellationToken cancellationToken = default)
        => await SyncAsync(false, cancellationToken);

    public async Task SyncAsync(bool force, CancellationToken cancellationToken)
    {
        await SyncLock.WaitAsync(cancellationToken);

        try
        {
            if (!force && await IsCatalogFreshAsync(cancellationToken))
            {
                return;
            }

            var companies =
                await GetCompaniesAsync(cancellationToken);

            if (companies.Count < ExpectedCatalogSize)
            {
                logger.LogWarning(
                    "KAP BIST şirket listesi beklenenden az " +
                    "kayıt döndürdü: {CompanyCount}.",
                    companies.Count);

                return;
            }

            var existingAssets =
                await dbContext.MarketAssets
                    .Where(asset =>
                        asset.AssetType == MarketAssetType.Equity
                        && asset.MarketCode == MarketCode
                        && asset.QuoteCurrency == QuoteCurrency
                        && asset.DataProviderCode ==
                            DataProviderCode
                        && asset.ProviderSymbol != null)
                    .ToListAsync(cancellationToken);

            var existingAssetLookup =
                existingAssets.ToDictionary(
                    asset => asset.ProviderSymbol!,
                    StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var company in companies)
            {
                var providerSymbol =
                    $"{company.Code}.IS";

                if (existingAssetLookup.TryGetValue(
                    providerSymbol,
                    out var existingAsset))
                {
                    existingAsset.Symbol = company.Code;
                    existingAsset.Name = company.Name;
                    existingAsset.IsActive = true;
                    existingAsset.UpdatedAtUtc = now;
                    updatedCount++;
                    continue;
                }

                var newAsset =
                    new MarketAsset
                    {
                        Symbol = company.Code,
                        Name = company.Name,
                        AssetType = MarketAssetType.Equity,
                        MarketCode = MarketCode,
                        QuoteCurrency = QuoteCurrency,
                        DataProviderCode = DataProviderCode,
                        ProviderSymbol = providerSymbol,
                        IsFeatured = false,
                        IsActive = true,
                        CreatedAtUtc = now
                    };

                dbContext.MarketAssets.Add(newAsset);
                existingAssetLookup.Add(
                    providerSymbol,
                    newAsset);

                addedCount++;
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            logger.LogInformation(
                "KAP BIST şirket kataloğu senkronize edildi. " +
                "Eklenen: {AddedCount}, güncellenen: {UpdatedCount}.",
                addedCount,
                updatedCount);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "KAP BIST şirket listesine ulaşılamadı.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "KAP BIST şirket listesi isteği " +
                "zaman aşımına uğradı.");
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
                    asset.AssetType == MarketAssetType.Equity
                    && asset.MarketCode == MarketCode
                    && asset.DataProviderCode ==
                        DataProviderCode
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

    private async Task<List<KapCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken)
    {
        using var response =
            await httpClient.GetAsync(
                "tr/bist-sirketler",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "KAP şirket listesi isteği {StatusCode} " +
                "koduyla başarısız oldu.",
                (int)response.StatusCode);

            return [];
        }

        var html =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        return ParseCompanies(html);
    }

    public static List<KapCompany> ParseCompanies(string html)
    {

        var companies =
            new List<KapCompany>();

        foreach (Match match in
            CompanyRowPattern.Matches(html))
        {
            var name = NormalizeName(
                WebUtility.HtmlDecode(
                    match.Groups["name"].Value));

            var codes =
                WebUtility.HtmlDecode(
                        Regex.Replace(match.Groups["codes"].Value, "</?div>", " ", RegexOptions.IgnoreCase))
                    .Split(
                        new[] { ',', ' ', '\t', '\r', '\n', '\u00a0' },
                        StringSplitOptions.RemoveEmptyEntries
                        | StringSplitOptions.TrimEntries);

            foreach (var value in codes)
            {
                var code =
                    value.ToUpperInvariant();

                if (StockCodePattern.IsMatch(code))
                {
                    companies.Add(
                        new KapCompany(code, name));
                }
            }
        }

        return companies
            .DistinctBy(
                company => company.Code,
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(company => company.Code)
            .ToList();
    }

    private static string NormalizeName(string name)
    {
        var normalized = name.Trim();

        return normalized.Length <= 200
            ? normalized
            : normalized[..200];
    }

    public sealed record KapCompany(
        string Code,
        string Name);
}
