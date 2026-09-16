using System.Globalization;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class TcmbMarketPriceProvider(
    ApplicationDbContext dbContext,
    HttpClient httpClient,
    ILogger<TcmbMarketPriceProvider> logger)
    : IMarketPriceProvider
{
    private const string ProviderCode = "TCMB_XML";
    private const string SourceName = "TCMB Orta Kur";
    private const int PreviousDaySearchLimit = 10;

    private static readonly TimeSpan RequestInterval =
        TimeSpan.FromHours(1);

    private static readonly SemaphoreSlim RefreshLock =
        new(1, 1);

    private static DateTime lastAttemptAtUtc =
        DateTime.MinValue;

    public async Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default)
    {
        if (marketAssetIds.Count == 0)
        {
            return;
        }

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

        await RefreshLock.WaitAsync(
            cancellationToken);

        try
        {
            var nowUtc =
                DateTime.UtcNow;

            if (nowUtc - lastAttemptAtUtc
                < RequestInterval)
            {
                return;
            }

            lastAttemptAtUtc = nowUtc;

            try
            {
                var currentDocument =
                    await LoadCurrentRatesAsync(
                        cancellationToken);

                var currentRoot =
                    currentDocument?.Root;

                if (currentRoot is null)
                {
                    return;
                }

                var publicationDate =
                    GetPublicationDate(
                        currentRoot);

                if (!publicationDate.HasValue)
                {
                    logger.LogWarning(
                        "TCMB kur tarihi okunamadı.");

                    return;
                }

                var observedAtUtc =
                    publicationDate.Value.ToDateTime(
                        new TimeOnly(12, 30),
                        DateTimeKind.Utc);

                var previousDocument =
                    await LoadPreviousRatesAsync(
                        publicationDate.Value,
                        cancellationToken);

                var previousRoot =
                    previousDocument?.Root;

                var assetIds =
                    assets
                        .Select(asset => asset.Id)
                        .ToList();

                var latestSnapshots =
                    await dbContext
                        .MarketPriceSnapshots
                        .Where(snapshot =>
                            assetIds.Contains(
                                snapshot.MarketAssetId)
                            && snapshot.Source
                                == SourceName)
                        .GroupBy(snapshot =>
                            snapshot.MarketAssetId)
                        .Select(group =>
                            group
                                .OrderByDescending(
                                    snapshot =>
                                        snapshot
                                            .ObservedAtUtc)
                                .ThenByDescending(
                                    snapshot =>
                                        snapshot.Id)
                                .First())
                        .ToListAsync(
                            cancellationToken);

                var latestSnapshotLookup =
                    latestSnapshots.ToDictionary(
                        snapshot =>
                            snapshot.MarketAssetId);

                var newSnapshots =
                    new List<MarketPriceSnapshot>();

                var hasUpdatedSnapshot =
                    false;

                foreach (var asset in assets)
                {
                    var currentCurrency =
                        FindCurrency(
                            currentRoot,
                            asset.ProviderSymbol!);

                    if (currentCurrency is null
                        || !TryGetMiddleRate(
                            currentCurrency,
                            out var currentMiddleRate))
                    {
                        continue;
                    }

                    decimal? dailyChangePercent =
                        null;

                    if (previousRoot is not null)
                    {
                        var previousCurrency =
                            FindCurrency(
                                previousRoot,
                                asset.ProviderSymbol!);

                        if (previousCurrency is not null
                            && TryGetMiddleRate(
                                previousCurrency,
                                out var previousMiddleRate)
                            && previousMiddleRate > 0)
                        {
                            dailyChangePercent =
                                decimal.Round(
                                    ((currentMiddleRate
                                        - previousMiddleRate)
                                        / previousMiddleRate)
                                    * 100m,
                                    6,
                                    MidpointRounding
                                        .AwayFromZero);
                        }
                    }

                    if (latestSnapshotLookup.TryGetValue(
                            asset.Id,
                            out var latestSnapshot)
                        && latestSnapshot.ObservedAtUtc
                            >= observedAtUtc)
                    {
                        if (latestSnapshot.ObservedAtUtc
                                == observedAtUtc
                            && dailyChangePercent.HasValue
                            && latestSnapshot
                                .DailyChangePercent
                                != dailyChangePercent)
                        {
                            latestSnapshot
                                .DailyChangePercent =
                                    dailyChangePercent;

                            hasUpdatedSnapshot = true;
                        }

                        continue;
                    }

                    newSnapshots.Add(
                        new MarketPriceSnapshot
                        {
                            MarketAssetId = asset.Id,
                            Price = currentMiddleRate,
                            DailyChangePercent =
                                dailyChangePercent,
                            PriceKind =
                                MarketPriceKind.Official,
                            Source = SourceName,
                            ObservedAtUtc =
                                observedAtUtc
                        });
                }

                if (newSnapshots.Count > 0)
                {
                    dbContext.MarketPriceSnapshots
                        .AddRange(newSnapshots);
                }

                if (newSnapshots.Count == 0
                    && !hasUpdatedSnapshot)
                {
                    return;
                }

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    exception,
                    "TCMB servisine ulaşılamadı.");
            }
            catch (XmlException exception)
            {
                logger.LogWarning(
                    exception,
                    "TCMB geçersiz XML döndürdü.");
            }
            catch (TaskCanceledException exception)
                when (!cancellationToken
                    .IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "TCMB kur isteği zaman aşımına uğradı.");
            }
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<XDocument?>
        LoadCurrentRatesAsync(
            CancellationToken cancellationToken)
    {
        using var response =
            await httpClient.GetAsync(
                "today.xml",
                HttpCompletionOption
                    .ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "TCMB kur isteği {StatusCode} " +
                "koduyla başarısız oldu.",
                (int)response.StatusCode);

            return null;
        }

        await using var responseStream =
            await response.Content
                .ReadAsStreamAsync(
                    cancellationToken);

        return await XDocument.LoadAsync(
            responseStream,
            LoadOptions.None,
            cancellationToken);
    }

    private async Task<XDocument?>
        LoadPreviousRatesAsync(
            DateOnly publicationDate,
            CancellationToken cancellationToken)
    {
        for (var dayOffset = 1;
             dayOffset <= PreviousDaySearchLimit;
             dayOffset++)
        {
            var candidateDate =
                publicationDate.AddDays(
                    -dayOffset);

            var yearAndMonth =
                candidateDate.ToString(
                    "yyyyMM",
                    CultureInfo.InvariantCulture);

            var fileName =
                candidateDate.ToString(
                    "ddMMyyyy",
                    CultureInfo.InvariantCulture);

            var requestUri =
                $"{yearAndMonth}/{fileName}.xml";

            using var response =
                await httpClient.GetAsync(
                    requestUri,
                    HttpCompletionOption
                        .ResponseHeadersRead,
                    cancellationToken);

            if (response.StatusCode
                == HttpStatusCode.NotFound)
            {
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TCMB geçmiş kur isteği " +
                    "{StatusCode} koduyla başarısız oldu.",
                    (int)response.StatusCode);

                return null;
            }

            await using var responseStream =
                await response.Content
                    .ReadAsStreamAsync(
                        cancellationToken);

            var document =
                await XDocument.LoadAsync(
                    responseStream,
                    LoadOptions.None,
                    cancellationToken);

            if (document.Root is not null)
            {
                return document;
            }
        }

        logger.LogWarning(
            "TCMB için önceki yayımlanmış " +
            "kur bülteni bulunamadı.");

        return null;
    }

    private static DateOnly? GetPublicationDate(
        XElement root)
    {
        var dateText =
            (string?)root.Attribute("Date");

        if (!DateOnly.TryParseExact(
                dateText,
                "MM/dd/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var publicationDate))
        {
            return null;
        }

        return publicationDate;
    }

    private static XElement? FindCurrency(
        XElement root,
        string currencyCode)
    {
        return root
            .Elements("Currency")
            .FirstOrDefault(element =>
                string.Equals(
                    (string?)element.Attribute(
                        "CurrencyCode"),
                    currencyCode,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetMiddleRate(
        XElement currency,
        out decimal middleRate)
    {
        middleRate = 0;

        if (!TryReadPositiveDecimal(
                currency,
                "ForexBuying",
                out var buying)
            || !TryReadPositiveDecimal(
                currency,
                "ForexSelling",
                out var selling))
        {
            return false;
        }

        var unit = 1m;

        if (TryReadPositiveDecimal(
                currency,
                "Unit",
                out var parsedUnit))
        {
            unit = parsedUnit;
        }

        middleRate =
            ((buying + selling) / 2m)
            / unit;

        return middleRate > 0;
    }

    private static bool TryReadPositiveDecimal(
        XElement parent,
        string elementName,
        out decimal value)
    {
        var text =
            parent.Element(elementName)?.Value;

        return decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value)
            && value > 0;
    }
}
