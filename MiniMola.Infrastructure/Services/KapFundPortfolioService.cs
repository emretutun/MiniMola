using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class KapFundPortfolioService(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    KapFundPortfolioPdfParser pdfParser,
    KapBistMarketAssetCatalogProvider catalogProvider,
    IMemoryCache memoryCache,
    ILogger<KapFundPortfolioService> logger)
    : IFundPortfolioService
{
    private static readonly SemaphoreSlim ImportLock =
        new(1, 1);

    private static readonly IReadOnlyDictionary<
        string,
        KapPortfolioReportDefinition> PilotReports =
            new Dictionary<
                string,
                KapPortfolioReportDefinition>(
                    StringComparer.OrdinalIgnoreCase)
            {
                ["THF"] = new(
                    new DateOnly(2026, 8, 31),
                    new DateTime(
                        2026,
                        9,
                        2,
                        8,
                        2,
                        16,
                        DateTimeKind.Utc),
                    1_657_113,
                    "4028328c9f52dc4001a06121e9864d61",
                    "THF_2026.08.pdf")
            };

    public async Task<FundPortfolioDto?> GetLatestAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default)
    {
        if (marketAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marketAssetId));
        }

        var asset =
            await dbContext.MarketAssets
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.Id == marketAssetId
                        && item.IsActive,
                    cancellationToken);

        if (asset is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(asset.ProviderSymbol)
            || !PilotReports.TryGetValue(
                asset.ProviderSymbol,
                out var definition))
        {
            return CreateUnsupportedResult(asset.Id);
        }

        var existing =
            await LoadReportAsync(
                asset.Id,
                definition.DocumentObjectId,
                cancellationToken);

        if (existing is not null)
        {
            await RelinkAsync(existing.Id, cancellationToken);
            return CreateResult((await LoadReportAsync(asset.Id, definition.DocumentObjectId, cancellationToken))!);
        }

        await ImportLock.WaitAsync(cancellationToken);

        try
        {
            existing =
                await LoadReportAsync(
                    asset.Id,
                    definition.DocumentObjectId,
                    cancellationToken);

            if (existing is not null)
            {
                return CreateResult(existing);
            }

            return await DownloadParseAndStoreAsync(
                asset,
                definition,
                cancellationToken);
        }
        finally
        {
            ImportLock.Release();
        }
    }

    private async Task<FundPortfolioDto>
        DownloadParseAndStoreAsync(
            MarketAsset fundAsset,
            KapPortfolioReportDefinition definition,
            CancellationToken cancellationToken)
    {
        try
        {
            var stockAssets =
                await dbContext.MarketAssets
                    .AsNoTracking()
                    .Where(asset =>
                        asset.IsActive
                        && asset.AssetType
                            == MarketAssetType.Equity
                        && asset.MarketCode == "BIST")
                    .ToListAsync(cancellationToken);

            var stockAssetLookup =
                stockAssets
                    .GroupBy(
                        asset => asset.Symbol,
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First(),
                        StringComparer.OrdinalIgnoreCase);

            using var response =
                await httpClient.GetAsync(
                    definition.DocumentPath,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "KAP {FundCode} portföy PDF isteği " +
                    "{StatusCode} koduyla başarısız oldu.",
                    fundAsset.Symbol,
                    (int)response.StatusCode);

                return CreateUnavailableResult(
                    fundAsset.Id,
                    "KAP portföy raporu indirilemedi.");
            }

            var contentLength =
                response.Content.Headers.ContentLength;

            if (contentLength is > 10_000_000)
            {
                return CreateUnavailableResult(
                    fundAsset.Id,
                    "KAP portföy raporu beklenenden büyük.");
            }

            var pdfBytes =
                await response.Content.ReadAsByteArrayAsync(
                    cancellationToken);

            var holdings =
                pdfParser.Parse(pdfBytes);

            if (holdings.Count == 0)
            {
                logger.LogWarning(
                    "KAP {FundCode} portföy PDF dosyasında " +
                    "eşleşen hisse satırı bulunamadı.",
                    fundAsset.Symbol);

                return CreateUnavailableResult(
                    fundAsset.Id,
                    "KAP raporu okundu fakat hisse satırları " +
                    "ayrıştırılamadı.");
            }

            var nowUtc = DateTime.UtcNow;

            var report =
                new FundPortfolioReport
                {
                    FundMarketAssetId = fundAsset.Id,
                    ReportDate = definition.ReportDate,
                    PublishedAtUtc = definition.PublishedAtUtc,
                    KapNotificationId =
                        definition.KapNotificationId,
                    DocumentObjectId =
                        definition.DocumentObjectId,
                    FileName = definition.FileName,
                    DocumentUrl = definition.DocumentUrl,
                    NotificationUrl =
                        definition.NotificationUrl,
                    ParserVersion =
                        KapFundPortfolioPdfParser.Version,
                    ParsedWeightPercent = decimal.Round(
                        holdings.Sum(item =>
                            item.WeightPercent),
                        2,
                        MidpointRounding.AwayFromZero),
                    MatchedWeightPercent = 0,
                    CreatedAtUtc = nowUtc
                };

            foreach (var holding in holdings)
            {
                stockAssetLookup.TryGetValue(
                    holding.Symbol,
                    out var matchedAsset);

                report.Holdings.Add(
                    new FundPortfolioHolding
                    {
                        SecuritySymbol = holding.Symbol,
                        SecurityName =
                            matchedAsset?.Name ?? holding.Symbol,
                        WeightPercent = holding.WeightPercent,
                        MatchedMarketAssetId = matchedAsset?.Id,
                        CreatedAtUtc = nowUtc
                    });
            }

            report.MatchedWeightPercent = decimal.Round(
                report.Holdings
                    .Where(holding =>
                        holding.MatchedMarketAssetId.HasValue)
                    .Sum(holding => holding.WeightPercent),
                2,
                MidpointRounding.AwayFromZero);

            dbContext.FundPortfolioReports.Add(report);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "KAP {FundCode} portföy raporu işlendi. " +
                "Hisse: {HoldingCount}, ağırlık: {WeightPercent}.",
                fundAsset.Symbol,
                holdings.Count,
                report.MatchedWeightPercent);

            return CreateResult(report);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "KAP {FundCode} portföy raporuna " +
                "ulaşılamadı.",
                fundAsset.Symbol);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "KAP {FundCode} portföy raporu işlenemedi.",
                fundAsset.Symbol);
        }

        return CreateUnavailableResult(
            fundAsset.Id,
            "KAP portföy raporu şu anda işlenemedi.");
    }

    private async Task RelinkAsync(int reportId, CancellationToken cancellationToken)
    {
        await ImportLock.WaitAsync(cancellationToken);
        try
        {
            var report = await dbContext.FundPortfolioReports.Include(x => x.Holdings)
                .SingleAsync(x => x.Id == reportId, cancellationToken);
            if (!report.Holdings.Any(x => x.MatchedMarketAssetId == null)) return;

            const string retryKey = "kap-holding-catalog-repair";
            if (!memoryCache.TryGetValue(retryKey, out _))
            {
                await catalogProvider.SyncAsync(true, cancellationToken);
                memoryCache.Set(retryKey, true, TimeSpan.FromMinutes(20));
            }
            var symbols = report.Holdings.Select(x => x.SecuritySymbol).ToArray();
            var assets = await dbContext.MarketAssets.AsNoTracking()
                .Where(x => x.IsActive && x.AssetType == MarketAssetType.Equity
                    && x.MarketCode == "BIST" && x.QuoteCurrency == "TRY"
                    && symbols.Contains(x.Symbol)).ToListAsync(cancellationToken);
            foreach (var holding in report.Holdings.Where(x => x.MatchedMarketAssetId == null))
            {
                var matches = assets.Where(x => x.Symbol == holding.SecuritySymbol).ToList();
                if (matches.Count != 1) continue;
                holding.MatchedMarketAssetId = matches[0].Id;
                holding.SecurityName = matches[0].Name;
            }
            report.MatchedWeightPercent = report.Holdings
                .Where(x => x.MatchedMarketAssetId != null).Sum(x => x.WeightPercent);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally { ImportLock.Release(); }
    }

    private Task<FundPortfolioReport?> LoadReportAsync(
        int fundMarketAssetId,
        string documentObjectId,
        CancellationToken cancellationToken)
    {
        return dbContext.FundPortfolioReports
            .AsNoTracking()
            .Include(report => report.Holdings)
            .SingleOrDefaultAsync(
                report =>
                    report.FundMarketAssetId
                        == fundMarketAssetId
                    && report.DocumentObjectId
                        == documentObjectId,
                cancellationToken);
    }

    private static FundPortfolioDto CreateResult(
        FundPortfolioReport report)
    {
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(3));

        return new FundPortfolioDto(
            report.FundMarketAssetId,
            true,
            true,
            report.ReportDate,
            report.PublishedAtUtc,
            Math.Max(
                0,
                today.DayNumber - report.ReportDate.DayNumber),
            report.ParsedWeightPercent,
            report.MatchedWeightPercent,
            report.NotificationUrl,
            report.DocumentUrl,
            null,
            report.Holdings
                .OrderByDescending(holding =>
                    holding.WeightPercent)
                .ThenBy(holding => holding.SecuritySymbol)
                .Select(holding =>
                    new FundPortfolioHoldingDto(
                        holding.SecuritySymbol,
                        holding.SecurityName,
                        holding.WeightPercent,
                        holding.MatchedMarketAssetId))
                .ToList());
    }

    private static FundPortfolioDto
        CreateUnsupportedResult(int marketAssetId)
    {
        return new FundPortfolioDto(
            marketAssetId,
            false,
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            null,
            "Ayrıntılı KAP portföy okuma pilotu " +
            "şimdilik THF için etkin.",
            []);
    }

    private static FundPortfolioDto
        CreateUnavailableResult(
            int marketAssetId,
            string message)
    {
        return new FundPortfolioDto(
            marketAssetId,
            true,
            false,
            null,
            null,
            null,
            0,
            0,
            null,
            null,
            message,
            []);
    }

    private sealed record KapPortfolioReportDefinition(
        DateOnly ReportDate,
        DateTime PublishedAtUtc,
        long KapNotificationId,
        string DocumentObjectId,
        string FileName)
    {
        public string DocumentPath =>
            $"tr/api/file/download/{DocumentObjectId}";

        public string DocumentUrl =>
            $"https://www.kap.org.tr/{DocumentPath}";

        public string NotificationUrl =>
            $"https://www.kap.org.tr/tr/Bildirim/" +
            KapNotificationId;
    }
}
