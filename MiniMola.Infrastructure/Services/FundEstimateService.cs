using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed partial class FundEstimateService(
    ApplicationDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IMarketPriceRefreshService marketPriceRefreshService,
    IMemoryCache memoryCache,
    IFundPortfolioService fundPortfolioService,
    ILogger<FundEstimateService> logger)
    : IFundEstimateService
{
    private const string ModelVersion = "allocation-proxy-v1";
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    private const string OfficialFundPriceSource =
        "TEFAS / BEFAS";

    private const string Methodology =
        "Son resmî fon fiyatı üzerine, açıklanan varlık " +
        "dağılımının eşleştirilebilen bölümündeki güncel " +
        "piyasa hareketleri ağırlıklandırılır.";

    private const decimal MinimumCoveragePercent = 25m;

    private static readonly HashSet<string>
        SupportedProviderCodes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "TEFAS_YAT",
                "TEFAS_EMK",
                "TEFAS_BYF"
            };

    private static readonly IReadOnlyDictionary<string, string>
        FundKinds =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["TEFAS_YAT"] = "YAT",
                ["TEFAS_EMK"] = "EMK",
                ["TEFAS_BYF"] = "BYF"
            };

    private static readonly IReadOnlyDictionary<string, string>
        CategoryLabels =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["bb"] = "Banka Bonosu",
                ["byf"] = "Borsa Yatırım Fonu",
                ["db"] = "Devlet Bonosu",
                ["dt"] = "Devlet Tahvili",
                ["eut"] = "Eurobond",
                ["fb"] = "Finansman Bonosu",
                ["hb"] = "Hazine Bonosu",
                ["hs"] = "Hisse Senedi",
                ["khau"] = "Altın Katılma Hesabı",
                ["km"] = "Kıymetli Madenler",
                ["r"] = "Repo",
                ["tpp"] = "Takasbank Para Piyasası",
                ["tr"] = "Ters Repo",
                ["vdm"] = "Vadeli Döviz Mevduatı",
                ["vint"] = "Vadeli İşlem Nakit Teminatı",
                ["vm"] = "Vadeli Mevduat",
                ["vmau"] = "Altın Mevduatı",
                ["vmd"] = "Döviz Mevduatı",
                ["vmtl"] = "TL Mevduatı",
                ["ybyf"] = "Yabancı Borsa Yatırım Fonu",
                ["yhs"] = "Yabancı Hisse Senedi",
                ["yyf"] = "Yatırım Fonu Katılma Payı"
            };

    private static readonly IReadOnlyDictionary<string, string>
        ProxySymbols =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["hs"] = "XU100",
                ["km"] = "GRAM_ALTIN",
                ["khau"] = "GRAM_ALTIN",
                ["vmau"] = "GRAM_ALTIN"
            };

    private static readonly HashSet<string>
        DistributionMetadataFields =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "fonKodu",
                "fonUnvan",
                "tarih",
                "bilFiyat"
            };

    public async Task<FundEstimateDto?> GetEstimateAsync(
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

        if (string.IsNullOrWhiteSpace(
                asset.DataProviderCode)
            || string.IsNullOrWhiteSpace(
                asset.ProviderSymbol)
            || !SupportedProviderCodes.Contains(
                asset.DataProviderCode))
        {
            return CreateUnsupportedResult(asset);
        }

        var portfolio = await fundPortfolioService.GetLatestAsync(asset.Id, cancellationToken);
        if (portfolio is { IsSupported: true })
            return await GetHoldingsEstimateAsync(asset, portfolio, cancellationToken);

        var distribution =
            await GetDistributionAsync(
                asset,
                cancellationToken);

        if (distribution is null)
        {
            return CreateUnavailableResult(
                asset,
                null,
                "Fonun güncel varlık dağılımı alınamadı.",
                []);
        }

        var categories =
            distribution.Weights
                .OrderByDescending(item => item.Value)
                .ToList();

        var requiredProxySymbols =
            categories
                .Select(category =>
                    ProxySymbols.GetValueOrDefault(
                        category.Key))
                .Where(symbol =>
                    !string.IsNullOrWhiteSpace(symbol))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        var proxyAssets =
            await dbContext.MarketAssets
                .AsNoTracking()
                .Where(item =>
                    item.IsActive
                    && item.IsFeatured
                    && requiredProxySymbols.Contains(
                        item.Symbol))
                .ToListAsync(cancellationToken);

        var refreshAssetIds =
            proxyAssets
                .Select(item => item.Id)
                .Append(asset.Id)
                .Distinct()
                .ToArray();

        await marketPriceRefreshService
            .RefreshStalePricesAsync(
                refreshAssetIds,
                cancellationToken);

        var snapshotAssetIds =
            refreshAssetIds.ToList();

        var latestSnapshots =
            await dbContext.MarketPriceSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    snapshotAssetIds.Contains(
                        snapshot.MarketAssetId))
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

        latestSnapshotLookup.TryGetValue(
            asset.Id,
            out var basePriceSnapshot);

        var proxyAssetLookup =
            proxyAssets.ToDictionary(
                item => item.Symbol,
                StringComparer.OrdinalIgnoreCase);

        var contributions =
            new List<FundEstimateContributionDto>();

        decimal estimatedChangePercent = 0;
        decimal coveragePercent = 0;

        foreach (var category in categories)
        {
            var categoryCode = category.Key;
            var weightPercent = category.Value;
            var proxySymbol =
                ProxySymbols.GetValueOrDefault(
                    categoryCode);

            MarketPriceSnapshot? proxySnapshot = null;

            if (proxySymbol is not null
                && proxyAssetLookup.TryGetValue(
                    proxySymbol,
                    out var proxyAsset))
            {
                latestSnapshotLookup.TryGetValue(
                    proxyAsset.Id,
                    out proxySnapshot);
            }

            var proxyIsNewEnough =
                proxySnapshot is not null
                && basePriceSnapshot is not null
                && proxySnapshot.ObservedAtUtc.Date
                    >= basePriceSnapshot.ObservedAtUtc.Date;

            var isCovered =
                proxyIsNewEnough
                && proxySnapshot!.DailyChangePercent.HasValue;

            decimal? contributionPercent = null;

            if (isCovered)
            {
                contributionPercent =
                    decimal.Round(
                        weightPercent
                        * proxySnapshot!
                            .DailyChangePercent!.Value
                        / 100m,
                        6,
                        MidpointRounding.AwayFromZero);

                estimatedChangePercent +=
                    contributionPercent.Value;

                coveragePercent += weightPercent;
            }

            contributions.Add(
                new FundEstimateContributionDto(
                    categoryCode.ToUpperInvariant(),
                    GetCategoryName(categoryCode),
                    weightPercent,
                    proxySymbol,
                    isCovered
                        ? proxySnapshot!.DailyChangePercent
                        : null,
                    contributionPercent,
                    isCovered
                        ? proxySnapshot!.ObservedAtUtc
                        : null,
                    isCovered));
        }

        coveragePercent =
            decimal.Round(
                Math.Min(coveragePercent, 100m),
                2,
                MidpointRounding.AwayFromZero);

        var isAvailable =
            basePriceSnapshot is not null
            && coveragePercent >= MinimumCoveragePercent;

        var confidence =
            GetConfidence(coveragePercent);

        if (!isAvailable)
        {
            return CreateUnavailableResult(
                asset,
                distribution.Date,
                basePriceSnapshot is null
                    ? "Tahmin için son resmî fon fiyatı bulunamadı."
                    : $"Tahmin için hesaplanabilen dağılım " +
                      $"%{coveragePercent:0.##}; en az " +
                      $"%{MinimumCoveragePercent:0} gerekiyor.",
                contributions,
                basePriceSnapshot,
                coveragePercent,
                confidence);
        }

        estimatedChangePercent =
            decimal.Round(
                estimatedChangePercent,
                4,
                MidpointRounding.AwayFromZero);

        var estimatedPrice =
            decimal.Round(
                basePriceSnapshot!.Price
                * (1m + estimatedChangePercent / 100m),
                8,
                MidpointRounding.AwayFromZero);

        var calculatedAtUtc = DateTime.UtcNow;

        var result = new FundEstimateDto(
            asset.Id,
            true,
            true,
            asset.QuoteCurrency,
            basePriceSnapshot.Price,
            basePriceSnapshot.ObservedAtUtc,
            estimatedChangePercent,
            estimatedPrice,
            coveragePercent,
            confidence.Code,
            confidence.Label,
            calculatedAtUtc,
            distribution.Date,
            Methodology,
            null,
            contributions);

        await SaveEstimateAsync(
            asset,
            result,
            cancellationToken);

        return result;
    }

    public async Task<FundEstimateHistoryDto?>
        GetHistoryAsync(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        if (marketAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marketAssetId));
        }

        var assetExists =
            await dbContext.MarketAssets
                .AsNoTracking()
                .AnyAsync(
                    asset =>
                        asset.Id == marketAssetId
                        && asset.IsActive,
                    cancellationToken);

        if (!assetExists)
        {
            return null;
        }

        await EvaluatePendingAsync(cancellationToken);

        var historyModel = await dbContext.FundPortfolioReports.AsNoTracking()
            .AnyAsync(x => x.FundMarketAssetId == marketAssetId, cancellationToken)
                ? HoldingsModelVersion : ModelVersion;

        var items =
            await dbContext.FundEstimateSnapshots
                .AsNoTracking()
                .Where(item =>
                    item.MarketAssetId == marketAssetId
                    && item.ModelVersion == historyModel
                    && item.Kind != FundEstimateKind.Legacy)
                .OrderByDescending(item => item.TargetDate)
                .ThenByDescending(item => item.CalculatedAtUtc)
                .Take(60)
                .ToListAsync(cancellationToken);

        var evaluatedItems =
            await dbContext.FundEstimateSnapshots.AsNoTracking()
                .Where(item => item.MarketAssetId == marketAssetId && item.ModelVersion == historyModel
                    && item.Kind == FundEstimateKind.Closing && item.AbsoluteErrorPercent.HasValue)
                .OrderByDescending(item => item.TargetDate)
                .Take(30)
                .ToListAsync(cancellationToken);

        decimal? meanAbsoluteErrorPercent =
            evaluatedItems.Count == 0
                ? null
                : decimal.Round(
                    evaluatedItems.Average(item =>
                        item.AbsoluteErrorPercent!.Value),
                    4,
                    MidpointRounding.AwayFromZero);

        decimal? withinOnePercentRate =
            evaluatedItems.Count == 0
                ? null
                : decimal.Round(
                    evaluatedItems.Count(item =>
                        item.AbsoluteErrorPercent <= 1m)
                    * 100m
                    / evaluatedItems.Count,
                    2,
                    MidpointRounding.AwayFromZero);

        return new FundEstimateHistoryDto(
            marketAssetId,
            evaluatedItems.Count,
            meanAbsoluteErrorPercent,
            withinOnePercentRate,
            items
                .Take(20)
                .Select(item =>
                    new FundEstimateHistoryItemDto(
                        item.TargetDate,
                        item.EstimatedChangePercent,
                        item.EstimatedPrice,
                        item.CoveragePercent,
                        item.ConfidenceCode,
                        item.CalculatedAtUtc,
                        item.ActualChangePercent,
                        item.ActualPrice,
                        item.ActualObservedAtUtc,
                        item.AbsoluteErrorPercent)
                    {
                        Kind = item.Kind == FundEstimateKind.Closing ? "closing" : "intraday",
                        Status = item.EvaluatedAtUtc != null ? "evaluated"
                            : item.TargetDate < GetTurkeyDate() ? "missing-official" : "pending"
                    })
                .ToList());
    }

    public async Task EvaluatePendingAsync(
        CancellationToken cancellationToken = default)
    {
        var today = GetTurkeyDate();

        var pendingEstimates =
            await dbContext.FundEstimateSnapshots
                .Where(item =>
                    (item.ModelVersion == ModelVersion || item.ModelVersion == HoldingsModelVersion)
                    && item.Kind != FundEstimateKind.Legacy
                    && item.EvaluatedAtUtc == null
                    && item.TargetDate <= today)
                .OrderByDescending(item => item.TargetDate)
                .Take(1_000)
                .ToListAsync(cancellationToken);

        if (pendingEstimates.Count == 0)
        {
            return;
        }

        var assetIds =
            pendingEstimates
                .Select(item => item.MarketAssetId)
                .Distinct()
                .ToList();

        var firstDate =
            pendingEstimates.Min(item => item.TargetDate);

        await marketPriceRefreshService.RefreshStalePricesAsync(assetIds, cancellationToken);

        var lastDate =
            pendingEstimates
                .Max(item => item.TargetDate)
                .AddDays(8);

        var firstObservedAtUtc =
            firstDate.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Utc);

        var lastObservedAtUtc =
            lastDate.ToDateTime(
                TimeOnly.MinValue,
                DateTimeKind.Utc);

        var officialPrices =
            await dbContext.MarketPriceSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    assetIds.Contains(snapshot.MarketAssetId)
                    && snapshot.PriceKind
                        == MarketPriceKind.Official
                    && snapshot.Source
                        == OfficialFundPriceSource
                    && snapshot.ObservedAtUtc
                        >= firstObservedAtUtc
                    && snapshot.ObservedAtUtc
                        < lastObservedAtUtc)
                .OrderBy(snapshot => snapshot.ObservedAtUtc)
                .ThenBy(snapshot => snapshot.Id)
                .ToListAsync(cancellationToken);

        var evaluatedAtUtc = DateTime.UtcNow;
        var hasChanges = false;

        foreach (var estimate in pendingEstimates)
        {
            var actualSnapshot =
                officialPrices.FirstOrDefault(snapshot =>
                    snapshot.MarketAssetId
                        == estimate.MarketAssetId
                    && snapshot.ObservedAtUtc
                        > estimate.BasePriceObservedAtUtc
                    && DateOnly.FromDateTime(
                        snapshot.ObservedAtUtc)
                        == estimate.TargetDate
                    && snapshot.CreatedAtUtc > estimate.CalculatedAtUtc
                    && snapshot.CreatedAtUtc <= evaluatedAtUtc);

            if (actualSnapshot is null
                || estimate.BasePrice <= 0)
            {
                continue;
            }

            hasChanges |= FundEstimateTrackingPolicy.TryEvaluate(estimate, actualSnapshot, evaluatedAtUtc);
        }

        if (hasChanges)
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
    }

    private async Task SaveEstimateAsync(
        MarketAsset asset, FundEstimateDto result, CancellationToken cancellationToken)
    {
        await SaveLock.WaitAsync(cancellationToken);
        try { await SaveEstimateCoreAsync(asset, result, cancellationToken); }
        finally { SaveLock.Release(); }
    }

    private async Task SaveEstimateCoreAsync(
        MarketAsset asset,
        FundEstimateDto result,
        CancellationToken cancellationToken)
    {
        var kind = FundEstimateTrackingPolicy.GetKind(result);
        if (kind is null) return;
        if (!result.IsAvailable
            || !result.BasePrice.HasValue
            || !result.BasePriceObservedAtUtc.HasValue
            || !result.EstimatedChangePercent.HasValue
            || !result.EstimatedPrice.HasValue)
        {
            return;
        }

        var basePriceDate =
            DateOnly.FromDateTime(
                result.BasePriceObservedAtUtc.Value);

        var targetDate =
            GetNextBusinessDay(basePriceDate);

        // Ağ veya kaynak gecikmesiyle eski bir fiyat üzerinden
        // geriye dönük "tahmin" üretilmesini engeller.
        if (targetDate < GetTurkeyDate())
        {
            return;
        }

        var existing =
            await dbContext.FundEstimateSnapshots
                .SingleOrDefaultAsync(
                    item =>
                        item.MarketAssetId == asset.Id
                        && item.TargetDate == targetDate
                        && item.ModelVersion == result.ModelVersion
                        && item.Kind == kind.Value,
                    cancellationToken);

        if (existing is not null && !FundEstimateTrackingPolicy.CanReplace(existing, result))
        {
            return;
        }

        if (existing is null)
        {
            existing = new FundEstimateSnapshot
            {
                MarketAssetId = asset.Id,
                TargetDate = targetDate,
                Kind = kind.Value,
                ModelVersion = result.ModelVersion,
                CreatedAtUtc = result.CalculatedAtUtc
            };

            dbContext.FundEstimateSnapshots.Add(existing);
        }

        existing.BasePrice = result.BasePrice.Value;
        existing.BasePriceObservedAtUtc =
            result.BasePriceObservedAtUtc.Value;
        existing.EstimatedChangePercent =
            result.EstimatedChangePercent.Value;
        existing.EstimatedPrice =
            result.EstimatedPrice.Value;
        existing.CoveragePercent = result.CoveragePercent;
        existing.ConfidenceCode = result.ConfidenceCode;
        existing.DistributionDate = result.DistributionDate;
        existing.CalculatedAtUtc = result.CalculatedAtUtc;
        existing.UpdatedAtUtc = result.CalculatedAtUtc;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly GetNextBusinessDay(
        DateOnly date)
    {
        var result = date.AddDays(1);

        while (result.DayOfWeek is
            DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            result = result.AddDays(1);
        }

        return result;
    }

    private static DateOnly GetTurkeyDate()
    {
        return DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(3));
    }

    private async Task<FundDistribution?>
        GetDistributionAsync(
            MarketAsset asset,
            CancellationToken cancellationToken)
    {
        var cacheKey =
            $"fund-distribution:{asset.Id}";

        if (memoryCache.TryGetValue<FundDistribution>(
                cacheKey,
                out var cachedDistribution)
            && cachedDistribution is not null)
        {
            return cachedDistribution;
        }

        try
        {
            if (!FundKinds.TryGetValue(
                    asset.DataProviderCode!,
                    out var fundKind))
            {
                return null;
            }

            var client =
                httpClientFactory.CreateClient(
                    MarketHistoryService.TefasClientName);

            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-10);

            var requestBody =
                new Dictionary<string, object?>
                {
                    ["fonTipi"] = fundKind,
                    ["fonKodu"] = asset.ProviderSymbol,
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
                    ["fonKod"] = asset.ProviderSymbol,
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
                await client.PostAsync(
                    "api/funds/dagilimSiraliGetirT",
                    requestContent,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TEFAS {Symbol} dağılım isteği " +
                    "{StatusCode} koduyla başarısız oldu.",
                    asset.Symbol,
                    (int)response.StatusCode);

                return null;
            }

            await using var responseStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var document =
                await JsonDocument.ParseAsync(
                    responseStream,
                    cancellationToken:
                        cancellationToken);

            var distributions =
                ParseDistributions(
                    document.RootElement,
                    asset.ProviderSymbol!);

            var result =
                distributions
                    .OrderByDescending(item => item.Date)
                    .FirstOrDefault();

            if (result is not null)
            {
                memoryCache.Set(
                    cacheKey,
                    result,
                    TimeSpan.FromHours(2));
            }

            return result;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "TEFAS {Symbol} dağılım servisine " +
                "ulaşılamadı.",
                asset.Symbol);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "TEFAS {Symbol} dağılım servisi " +
                "geçersiz veri döndürdü.",
                asset.Symbol);
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "TEFAS {Symbol} dağılım isteği " +
                "zaman aşımına uğradı.",
                asset.Symbol);
        }

        return null;
    }

    private static IReadOnlyList<FundDistribution>
        ParseDistributions(
            JsonElement root,
            string providerSymbol)
    {
        if (!root.TryGetProperty(
                "resultList",
                out var resultList)
            || resultList.ValueKind
                != JsonValueKind.Array)
        {
            return [];
        }

        var result =
            new List<FundDistribution>();

        foreach (var row in resultList.EnumerateArray())
        {
            if (!row.TryGetProperty(
                    "fonKodu",
                    out var fundCodeElement)
                || !string.Equals(
                    fundCodeElement.GetString(),
                    providerSymbol,
                    StringComparison.OrdinalIgnoreCase)
                || !row.TryGetProperty(
                    "tarih",
                    out var dateElement)
                || !DateOnly.TryParseExact(
                    dateElement.GetString(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date))
            {
                continue;
            }

            var weights =
                new Dictionary<string, decimal>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var property in
                row.EnumerateObject())
            {
                if (DistributionMetadataFields.Contains(
                        property.Name)
                    || property.Value.ValueKind
                        != JsonValueKind.Number
                    || !property.Value.TryGetDecimal(
                        out var weight)
                    || weight <= 0)
                {
                    continue;
                }

                weights[property.Name] = weight;
            }

            if (weights.Count > 0)
            {
                result.Add(
                    new FundDistribution(
                        date,
                        weights));
            }
        }

        return result;
    }

    private static string GetCategoryName(
        string categoryCode)
    {
        return CategoryLabels.GetValueOrDefault(
                   categoryCode)
               ?? categoryCode.ToUpperInvariant();
    }

    private static (string Code, string Label)
        GetConfidence(decimal coveragePercent)
    {
        return coveragePercent switch
        {
            >= 85m => ("high", "Yüksek"),
            >= 60m => ("medium", "Orta"),
            >= MinimumCoveragePercent => ("low", "Düşük"),
            _ => ("insufficient", "Yetersiz")
        };
    }

    private static FundEstimateDto
        CreateUnsupportedResult(MarketAsset asset)
    {
        return new FundEstimateDto(
            asset.Id,
            false,
            false,
            asset.QuoteCurrency,
            null,
            null,
            null,
            null,
            0,
            "unsupported",
            "Desteklenmiyor",
            DateTime.UtcNow,
            null,
            Methodology,
            "Günlük tahmin yalnızca TEFAS/BEFAS " +
            "fonları için hazırlanır.",
            []);
    }

    private static FundEstimateDto
        CreateUnavailableResult(
            MarketAsset asset,
            DateOnly? distributionDate,
            string message,
            IReadOnlyList<
                FundEstimateContributionDto> contributions,
            MarketPriceSnapshot? basePriceSnapshot = null,
            decimal coveragePercent = 0,
            (string Code, string Label)? confidence = null)
    {
        var confidenceValue =
            confidence
            ?? GetConfidence(coveragePercent);

        return new FundEstimateDto(
            asset.Id,
            true,
            false,
            asset.QuoteCurrency,
            basePriceSnapshot?.Price,
            basePriceSnapshot?.ObservedAtUtc,
            null,
            null,
            coveragePercent,
            confidenceValue.Code,
            confidenceValue.Label,
            DateTime.UtcNow,
            distributionDate,
            Methodology,
            message,
            contributions);
    }

    private sealed record FundDistribution(
        DateOnly Date,
        IReadOnlyDictionary<string, decimal> Weights);
}
