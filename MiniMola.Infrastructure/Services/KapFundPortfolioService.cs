using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Json;
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

    public async Task RefreshReportsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.MarketAssets.AsNoTracking()
            .Where(x => x.IsActive && x.DataProviderCode == "TEFAS_YAT"
                && (dbContext.UserFavoriteAssets.Any(f => f.MarketAssetId == x.Id)
                    || dbContext.FundPortfolioReports.Any(r => r.FundMarketAssetId == x.Id)))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        foreach (var id in ids) await GetLatestAsync(id, cancellationToken);
    }

    public async Task<FundPortfolioDto?> GetLatestAsync(int marketAssetId, CancellationToken cancellationToken = default)
    {
        var result = await GetLatestCoreAsync(marketAssetId, cancellationToken);
        if (result is not null) memoryCache.Set($"kap-health:{marketAssetId}", result, TimeSpan.FromDays(1));
        return result;
    }

    private async Task<FundPortfolioDto?> GetLatestCoreAsync(
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

        if (string.IsNullOrWhiteSpace(asset.ProviderSymbol) || asset.DataProviderCode != "TEFAS_YAT")
        {
            return CreateUnsupportedResult(asset.Id);
        }

        var cacheKey = $"kap-report-check:{asset.Id}";
        var existing = await LoadReportAsync(asset.Id, cancellationToken);
        if (existing is not null) await RelinkAsync(existing.Id, cancellationToken);
        await ImportLock.WaitAsync(cancellationToken);
        try
        {
            existing = await LoadReportAsync(asset.Id, cancellationToken);
            if (memoryCache.TryGetValue<string>(cacheKey, out var cachedMessage))
                return existing is null ? CreateUnavailableResult(asset.Id, cachedMessage!)
                    : CreateResult(existing) with { Message = cachedMessage };

            try
            {
                var catalog = await GetEquityCatalogAsync(cancellationToken);
                if (!catalog.TryGetValue(asset.ProviderSymbol, out var fund))
                    return CreateUnsupportedResult(asset.Id);
                var now = DateTime.UtcNow;
                var today = DateOnly.FromDateTime(now.AddHours(3));
                using var response = await httpClient.PostAsJsonAsync("tr/api/disclosure/funds/byCriteria", new
                {
                    fromDate = today.AddMonths(-3).ToString("yyyy-MM-dd"),
                    toDate = today.ToString("yyyy-MM-dd"),
                    fundTypeList = new[] { "SYF" },
                    mkkMemberOidList = Array.Empty<string>(),
                    fundOidList = new[] { fund.Oid },
                    passiveFundOidList = Array.Empty<string>(),
                    disclosureClass = "", isLate = "", subjectList = Array.Empty<string>(),
                    discIndex = Array.Empty<int>(), fromSrc = false, srcCategory = ""
                }, cancellationToken);
                response.EnsureSuccessStatusCode();
                var notificationId = KapPortfolioDiscovery.FindLatest(
                    await response.Content.ReadAsStringAsync(cancellationToken), now, fund.Code)
                    ?? throw new InvalidDataException("Son üç ayda portföy raporu bulunamadı.");
                var detailJson = await httpClient.GetStringAsync(
                    $"tr/api/notification/attachment-detail/{notificationId}", cancellationToken);
                var definition = KapPortfolioDiscovery.ReadDetail(detailJson, notificationId, now, fund.Code, fund.Oid);
                if (existing is not null && definition.DocumentObjectId != existing.DocumentObjectId
                    && definition.ReportDate <= existing.ReportDate)
                    throw new InvalidDataException("Aynı dönem düzeltmesi veya eski rapor için manuel inceleme gerekiyor.");

                var result = existing is not null && definition.DocumentObjectId == existing.DocumentObjectId
                    ? CreateResult(existing)
                    : await DownloadParseAndStoreAsync(asset, definition, cancellationToken);
                if (!result.IsAvailable)
                {
                    var failureMessage = result.Message + " " + (existing is null
                        ? "İçerik bazlı tahmin üretilmedi."
                        : "Son geçerli rapor kullanılıyor.") + " Kontrol 30 dakika sonra yeniden denenecek.";
                    memoryCache.Set(cacheKey, failureMessage, TimeSpan.FromMinutes(30));
                    return existing is null ? result with { Message = failureMessage }
                        : CreateResult(existing) with { Message = failureMessage };
                }
                var message = $"KAP otomatik kontrolü: {now.AddHours(3):dd.MM.yyyy HH:mm} (TSİ).";
                memoryCache.Set(cacheKey, message, TimeSpan.FromHours(6));
                return result with { Message = message };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "{FundCode} otomatik KAP kontrolü başarısız; son geçerli rapor korunuyor.", asset.ProviderSymbol);
                var message = existing is null
                    ? "KAP raporu/formatı doğrulanamadı; içerik bazlı tahmin üretilmedi. Kontrol 30 dakika sonra yeniden denenecek."
                    : "Yeni KAP raporu kontrolü tamamlanamadı; son geçerli rapor kullanılıyor. 30 dakika sonra yeniden denenecek.";
                if (exception is InvalidDataException)
                    message = exception.Message + " " + (existing is null ? "İçerik bazlı tahmin üretilmedi. " : "Son geçerli rapor kullanılıyor. ")
                        + "Rapor/okuyucu kontrolü gerekiyor; yalnız beklemek bu sorunu çözmeyebilir.";
                memoryCache.Set(cacheKey, message, TimeSpan.FromMinutes(30));
                return existing is null ? CreateUnavailableResult(asset.Id, message)
                    : CreateResult(existing) with { Message = message };
            }
        }
        finally
        {
            ImportLock.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, KapEquityFund>> GetEquityCatalogAsync(CancellationToken cancellationToken)
    {
        const string key = "kap-domestic-equity-fund-catalog-v1";
        if (memoryCache.TryGetValue<IReadOnlyDictionary<string, KapEquityFund>>(key, out var cached)) return cached!;
        // Throttle upstream failures across different fund requests as well.
        if (memoryCache.TryGetValue(key + ":failed", out _)) throw new HttpRequestException("KAP kataloğu beklemede.");
        try
        {
            var html = await httpClient.GetStringAsync("tr/YatirimFonlari/YF", cancellationToken);
            var result = KapPortfolioDiscovery.ReadEquityCatalog(html);
            memoryCache.Set(key, result, TimeSpan.FromHours(24));
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            memoryCache.Set(key + ":failed", true, TimeSpan.FromMinutes(5));
            throw;
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
                        && asset.MarketCode == "BIST" && asset.QuoteCurrency == "TRY")
                    .ToListAsync(cancellationToken);

            var stockAssetLookup =
                stockAssets
                    .GroupBy(
                        asset => asset.Symbol,
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() == 1)
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

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > 10_000_000) throw new InvalidDataException("KAP PDF boyut sınırı aşıldı.");
                buffer.Write(chunk, 0, read);
            }
            var pdfBytes = buffer.ToArray();

            var holdings =
                pdfParser.ParseValidated(pdfBytes, fundAsset.ProviderSymbol!, definition.ReportDate);

            if (!KapPortfolioDiscovery.HasValidHoldings(holdings))
            {
                logger.LogWarning(
                    "KAP {FundCode} portföy PDF dosyasında " +
                    "eşleşen hisse satırı bulunamadı.",
                    fundAsset.Symbol);

                return CreateUnavailableResult(
                    fundAsset.Id,
                    "KAP raporunun hisse satırları veya toplam ağırlığı doğrulanamadı.");
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
            try { await dbContext.SaveChangesAsync(cancellationToken); }
            catch
            {
                // Do not leave failed inserts tracked for another service's SaveChanges.
                foreach (var holding in report.Holdings) dbContext.Entry(holding).State = EntityState.Detached;
                dbContext.Entry(report).State = EntityState.Detached;
                throw;
            }

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
        catch (InvalidDataException) { throw; }
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
        CancellationToken cancellationToken)
    {
        return dbContext.FundPortfolioReports
            .AsNoTracking()
            .Include(report => report.Holdings)
            .Where(report => report.FundMarketAssetId == fundMarketAssetId)
            .OrderByDescending(report => report.ReportDate)
            .ThenByDescending(report => report.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
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
            "İçerik bazlı KAP okuma, katalogda yerli hisse fonu olarak doğrulanan yatırım fonları için etkin. " +
            "Yabancı, serbest ve arbitraj fonları bu kapsamda değil.",
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

}
