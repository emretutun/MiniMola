using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class MarketHistoryService(
    ApplicationDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache,
    ILogger<MarketHistoryService> logger)
    : IMarketHistoryService
{
    public const string YahooClientName =
        "MarketHistoryYahoo";

    public const string CoinGeckoClientName =
        "MarketHistoryCoinGecko";

    public const string TefasClientName =
        "MarketHistoryTefas";

    private const string YahooProviderCode =
        "YAHOO_FINANCE";

    private const string CoinGeckoProviderCode =
        "COINGECKO";

    private const string TcmbProviderCode =
        "TCMB_XML";

    private static readonly HashSet<string>
        TefasProviderCodes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "TEFAS_YAT",
                "TEFAS_EMK",
                "TEFAS_BYF"
            };

    private const string GramGoldSymbol =
        "GRAM_ALTIN";

    private const decimal TroyOunceInGrams =
        31.1034768m;

    private static readonly IReadOnlyDictionary<
        string,
        HistoryRange> SupportedRanges =
        new Dictionary<string, HistoryRange>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["1w"] = new(
                "1w",
                "5d",
                "30m",
                7,
                1,
                "30 dakika"),

            ["1m"] = new(
                "1m",
                "1mo",
                "1d",
                30,
                1,
                "1 gün"),

            ["3m"] = new(
                "3m",
                "3mo",
                "1d",
                90,
                3,
                "1 gün"),

            ["6m"] = new(
                "6m",
                "6mo",
                "1d",
                180,
                6,
                "1 gün"),

            ["1y"] = new(
                "1y",
                "1y",
                "1d",
                365,
                12,
                "1 gün")
        };

    public async Task<MarketHistoryResultDto?>
        GetHistoryAsync(
            int marketAssetId,
            string? range,
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

        var latestPrice =
            await dbContext.MarketPriceSnapshots
                .AsNoTracking()
                .Where(snapshot =>
                    snapshot.MarketAssetId
                    == marketAssetId)
                .OrderByDescending(snapshot =>
                    snapshot.ObservedAtUtc)
                .ThenByDescending(snapshot =>
                    snapshot.Id)
                .FirstOrDefaultAsync(
                    cancellationToken);

        var selectedRange =
            GetRange(range);

        var providerCode =
            asset.DataProviderCode?.Trim();

        var foreignExchangeHistorySymbol =
            GetForeignExchangeHistorySymbol(asset);

        var isSupported =
            foreignExchangeHistorySymbol is not null
            || IsTefasProvider(providerCode)
            || ((providerCode is YahooProviderCode
                    or CoinGeckoProviderCode)
                && !string.IsNullOrWhiteSpace(
                    asset.ProviderSymbol));

        IReadOnlyList<MarketHistoryPointDto> points =
            [];

        var historySource =
            string.Equals(
                asset.Symbol,
                GramGoldSymbol,
                StringComparison.OrdinalIgnoreCase)
                ? "CoinGecko PAXG/Gram"
                : foreignExchangeHistorySymbol is not null
                    ? "Yahoo Finance (grafik)"
                    : IsTefasProvider(providerCode)
                        ? "TEFAS / BEFAS"
                    : providerCode switch
            {
                YahooProviderCode => "Yahoo Finance",
                CoinGeckoProviderCode => "CoinGecko",
                _ => ""
            };

        if (isSupported)
        {
            var cacheKey =
                $"market-history:{asset.Id}:" +
                $"{selectedRange.Code}";

            if (memoryCache.TryGetValue<
                    IReadOnlyList<
                        MarketHistoryPointDto>>(
                    cacheKey,
                    out var cachedPoints)
                && cachedPoints is not null)
            {
                points = cachedPoints;
            }
            else
            {
                points =
                    await LoadPointsAsync(
                        asset,
                        selectedRange,
                        cancellationToken);

                if (points.Count > 0)
                {
                    memoryCache.Set(
                        cacheKey,
                        points,
                        TimeSpan.FromMinutes(5));
                }
            }
        }

        var message =
            !isSupported
                ? "Bu varlık için geçmiş grafik desteği " +
                  "henüz eklenmedi."
                : points.Count == 0
                    ? "Geçmiş fiyat verisi şu anda alınamadı."
                    : null;

        return new MarketHistoryResultDto(
            asset.Id,
            asset.Symbol,
            asset.Name,
            asset.AssetType,
            asset.MarketCode,
            asset.QuoteCurrency,
            latestPrice?.Price,
            latestPrice?.DailyChangePercent,
            latestPrice?.PriceKind,
            latestPrice?.Source,
            latestPrice?.ObservedAtUtc,
            selectedRange.Code,
            selectedRange.DisplayInterval,
            historySource,
            isSupported,
            message,
            points);
    }

    private async Task<IReadOnlyList<
        MarketHistoryPointDto>> LoadPointsAsync(
            MarketAsset asset,
            HistoryRange range,
            CancellationToken cancellationToken)
    {
        try
        {
            var foreignExchangeHistorySymbol =
                GetForeignExchangeHistorySymbol(asset);

            if (foreignExchangeHistorySymbol is not null)
            {
                return await LoadYahooPointsAsync(
                    foreignExchangeHistorySymbol,
                    range,
                    cancellationToken);
            }

            var points =
                IsTefasProvider(asset.DataProviderCode)
                    ? await LoadTefasPointsAsync(
                        asset.ProviderSymbol!,
                        range,
                        cancellationToken)
                    : asset.DataProviderCode switch
            {
                YahooProviderCode =>
                    await LoadYahooPointsAsync(
                        asset.ProviderSymbol!,
                        range,
                        cancellationToken),

                CoinGeckoProviderCode =>
                    await LoadCoinGeckoPointsAsync(
                        asset.ProviderSymbol!,
                        asset.QuoteCurrency,
                        range,
                        cancellationToken),

                _ => []
            };

            if (!string.Equals(
                    asset.Symbol,
                    GramGoldSymbol,
                    StringComparison.OrdinalIgnoreCase))
            {
                return points;
            }

            return points
                .Select(point =>
                    point with
                    {
                        Open = DivideByTroyOunce(
                            point.Open),
                        High = DivideByTroyOunce(
                            point.High),
                        Low = DivideByTroyOunce(
                            point.Low),
                        Close =
                            point.Close
                            / TroyOunceInGrams
                    })
                .ToList();
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "{Symbol} geçmiş fiyat servisine " +
                "ulaşılamadı.",
                asset.Symbol);

            return [];
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "{Symbol} geçmiş fiyat servisi " +
                "geçersiz veri döndürdü.",
                asset.Symbol);

            return [];
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken
                .IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "{Symbol} geçmiş fiyat isteği " +
                "zaman aşımına uğradı.",
                asset.Symbol);

            return [];
        }
    }

    private async Task<IReadOnlyList<
        MarketHistoryPointDto>> LoadTefasPointsAsync(
            string providerSymbol,
            HistoryRange range,
            CancellationToken cancellationToken)
    {
        var client =
            httpClientFactory.CreateClient(
                TefasClientName);

        var requestBody =
            new
            {
                fonKodu = providerSymbol,
                dil = "TR",
                periyod = range.TefasMonths
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
                "api/funds/fonFiyatBilgiGetir",
                requestContent,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "TEFAS {Symbol} geçmiş fiyat isteği " +
                "{StatusCode} koduyla başarısız oldu.",
                providerSymbol,
                (int)response.StatusCode);

            return [];
        }

        var result =
            await response.Content.ReadFromJsonAsync<
                TefasHistoryResponse>(
                    cancellationToken:
                        cancellationToken);

        if (result?.ResultList is null)
        {
            return [];
        }

        var firstIncludedDate =
            DateOnly.FromDateTime(
                DateTime.UtcNow.Date.AddDays(
                    -range.CoinGeckoDays));

        return result.ResultList
            .Select(row =>
            {
                var hasDate = DateOnly.TryParseExact(
                    row.Date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var date);

                if (!hasDate
                    || date < firstIncludedDate
                    || row.Price <= 0)
                {
                    return null;
                }

                var time =
                    new DateTimeOffset(
                        date.ToDateTime(
                            TimeOnly.MinValue,
                            DateTimeKind.Utc))
                        .ToUnixTimeSeconds();

                return new MarketHistoryPointDto(
                    time,
                    null,
                    null,
                    null,
                    row.Price,
                    null);
            })
            .Where(point => point is not null)
            .Select(point => point!)
            .OrderBy(point => point.Time)
            .ToList();
    }

    private async Task<IReadOnlyList<
        MarketHistoryPointDto>> LoadYahooPointsAsync(
            string providerSymbol,
            HistoryRange range,
            CancellationToken cancellationToken)
    {
        var client =
            httpClientFactory.CreateClient(
                YahooClientName);

        var encodedSymbol =
            Uri.EscapeDataString(providerSymbol);

        var requestUri =
            $"v8/finance/chart/{encodedSymbol}" +
            $"?range={range.YahooRange}" +
            $"&interval={range.YahooInterval}" +
            "&includePrePost=false";

        using var response =
            await client.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Yahoo Finance {Symbol} geçmiş fiyat " +
                "isteği {StatusCode} koduyla başarısız oldu.",
                providerSymbol,
                (int)response.StatusCode);

            return [];
        }

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken:
                    cancellationToken);

        if (!TryGetYahooResult(
                document.RootElement,
                out var result)
            || !result.TryGetProperty(
                "timestamp",
                out var timestamps)
            || timestamps.ValueKind
                != JsonValueKind.Array
            || !result.TryGetProperty(
                "indicators",
                out var indicators)
            || !indicators.TryGetProperty(
                "quote",
                out var quotes)
            || quotes.ValueKind
                != JsonValueKind.Array
            || quotes.GetArrayLength() == 0)
        {
            return [];
        }

        var quote = quotes[0];

        if (!quote.TryGetProperty(
                "close",
                out var closes)
            || closes.ValueKind
                != JsonValueKind.Array)
        {
            return [];
        }

        quote.TryGetProperty("open", out var opens);
        quote.TryGetProperty("high", out var highs);
        quote.TryGetProperty("low", out var lows);
        quote.TryGetProperty("volume", out var volumes);

        var pointCount =
            Math.Min(
                timestamps.GetArrayLength(),
                closes.GetArrayLength());

        var points =
            new List<MarketHistoryPointDto>(
                pointCount);

        for (var index = 0;
             index < pointCount;
             index++)
        {
            var timestampElement =
                timestamps[index];

            var close =
                GetDecimal(closes, index);

            if (!timestampElement.TryGetInt64(
                    out var timestamp)
                || timestamp <= 0
                || !close.HasValue
                || close.Value <= 0)
            {
                continue;
            }

            points.Add(
                new MarketHistoryPointDto(
                    timestamp,
                    GetDecimal(opens, index),
                    GetDecimal(highs, index),
                    GetDecimal(lows, index),
                    close.Value,
                    GetDecimal(volumes, index)));
        }

        return points;
    }

    private async Task<IReadOnlyList<
        MarketHistoryPointDto>> LoadCoinGeckoPointsAsync(
            string providerSymbol,
            string quoteCurrency,
            HistoryRange range,
            CancellationToken cancellationToken)
    {
        var client =
            httpClientFactory.CreateClient(
                CoinGeckoClientName);

        var encodedSymbol =
            Uri.EscapeDataString(providerSymbol);

        var encodedCurrency =
            Uri.EscapeDataString(
                quoteCurrency.ToLowerInvariant());

        var requestUri =
            $"coins/{encodedSymbol}/market_chart" +
            $"?vs_currency={encodedCurrency}" +
            $"&days={range.CoinGeckoDays}" +
            "&precision=full";

        using var response =
            await client.GetAsync(
                requestUri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "CoinGecko {Symbol} geçmiş fiyat " +
                "isteği {StatusCode} koduyla başarısız oldu.",
                providerSymbol,
                (int)response.StatusCode);

            return [];
        }

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(
                cancellationToken);

        using var document =
            await JsonDocument.ParseAsync(
                responseStream,
                cancellationToken:
                    cancellationToken);

        if (!document.RootElement.TryGetProperty(
                "prices",
                out var prices)
            || prices.ValueKind
                != JsonValueKind.Array)
        {
            return [];
        }

        var volumeLookup =
            CreateCoinGeckoVolumeLookup(
                document.RootElement);

        var points =
            new List<MarketHistoryPointDto>(
                prices.GetArrayLength());

        foreach (var priceRow in prices.EnumerateArray())
        {
            if (priceRow.ValueKind
                    != JsonValueKind.Array
                || priceRow.GetArrayLength() < 2
                || !priceRow[0].TryGetInt64(
                    out var timestampMilliseconds)
                || !priceRow[1].TryGetDecimal(
                    out var price)
                || timestampMilliseconds <= 0
                || price <= 0)
            {
                continue;
            }

            volumeLookup.TryGetValue(
                timestampMilliseconds,
                out var volume);

            points.Add(
                new MarketHistoryPointDto(
                    timestampMilliseconds / 1000,
                    null,
                    null,
                    null,
                    price,
                    volume));
        }

        return points;
    }

    private static Dictionary<long, decimal?>
        CreateCoinGeckoVolumeLookup(
            JsonElement root)
    {
        var result =
            new Dictionary<long, decimal?>();

        if (!root.TryGetProperty(
                "total_volumes",
                out var volumes)
            || volumes.ValueKind
                != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var volumeRow in volumes.EnumerateArray())
        {
            if (volumeRow.ValueKind
                    != JsonValueKind.Array
                || volumeRow.GetArrayLength() < 2
                || !volumeRow[0].TryGetInt64(
                    out var timestampMilliseconds)
                || !volumeRow[1].TryGetDecimal(
                    out var volume))
            {
                continue;
            }

            result[timestampMilliseconds] = volume;
        }

        return result;
    }

    private static bool TryGetYahooResult(
        JsonElement root,
        out JsonElement result)
    {
        result = default;

        return root.TryGetProperty(
                   "chart",
                   out var chart)
               && chart.TryGetProperty(
                   "result",
                   out var results)
               && results.ValueKind
                   == JsonValueKind.Array
               && results.GetArrayLength() > 0
               && (result = results[0]).ValueKind
                   == JsonValueKind.Object;
    }

    private static decimal? GetDecimal(
        JsonElement array,
        int index)
    {
        if (array.ValueKind
                != JsonValueKind.Array
            || index < 0
            || index >= array.GetArrayLength())
        {
            return null;
        }

        var item = array[index];

        return item.ValueKind == JsonValueKind.Number
               && item.TryGetDecimal(out var value)
            ? value
            : null;
    }

    private static HistoryRange GetRange(
        string? requestedRange)
    {
        if (!string.IsNullOrWhiteSpace(
                requestedRange)
            && SupportedRanges.TryGetValue(
                requestedRange.Trim(),
                out var result))
        {
            return result;
        }

        return SupportedRanges["1m"];
    }

    private static string?
        GetForeignExchangeHistorySymbol(
            MarketAsset asset)
    {
        if (!string.Equals(
                asset.DataProviderCode,
                TcmbProviderCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return asset.Symbol.ToUpperInvariant() switch
        {
            "USDTRY" => "TRY=X",
            "EURTRY" => "EURTRY=X",
            _ => null
        };
    }

    private static bool IsTefasProvider(
        string? providerCode)
    {
        return !string.IsNullOrWhiteSpace(providerCode)
               && TefasProviderCodes.Contains(providerCode);
    }

    private static decimal? DivideByTroyOunce(
        decimal? value)
    {
        return value.HasValue
            ? value.Value / TroyOunceInGrams
            : null;
    }

    private sealed record HistoryRange(
        string Code,
        string YahooRange,
        string YahooInterval,
        int CoinGeckoDays,
        int TefasMonths,
        string DisplayInterval);

    private sealed record TefasHistoryResponse(
        [property: JsonPropertyName("resultList")]
        IReadOnlyList<TefasHistoryRow>? ResultList);

    private sealed record TefasHistoryRow(
        [property: JsonPropertyName("tarih")]
        string Date,

        [property: JsonPropertyName("fiyat")]
        decimal Price);
}
