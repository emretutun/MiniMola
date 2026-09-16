namespace MiniMola.Application.Markets;

public sealed class MarketTechnicalAnalysisService(
    IMarketHistoryService marketHistoryService)
    : IMarketTechnicalAnalysisService
{
    private const int RsiPeriod = 14;
    private const int MacdFastPeriod = 12;
    private const int MacdSlowPeriod = 26;
    private const int MacdSignalPeriod = 9;

    public async Task<MarketTechnicalAnalysisDto?>
        GetAnalysisAsync(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        var history =
            await marketHistoryService.GetHistoryAsync(
                marketAssetId,
                "1y",
                cancellationToken);

        if (history is null)
        {
            return null;
        }

        var closes =
            history.Points
                .OrderBy(point => point.Time)
                .Select(point => point.Close)
                .Where(value => value > 0)
                .ToList();

        if (!history.IsSupported ||
            closes.Count < 35)
        {
            return CreateUnavailableResult(
                marketAssetId,
                history.QuoteCurrency,
                closes.Count,
                "Teknik görünüm için en az 35 geçerli " +
                "fiyat noktası gerekiyor.");
        }

        var price = closes[^1];
        var sma20 = CalculateSma(closes, 20);
        var sma50 = CalculateSma(closes, 50);
        var sma200 = CalculateSma(closes, 200);
        var rsi = CalculateRsi(closes, RsiPeriod);
        var macd = CalculateMacd(closes);

        var score = 50;
        var indicators =
            new List<MarketTechnicalIndicatorDto>();

        score += CreateMovingAverageIndicator(
            indicators,
            price,
            sma20,
            sma50);

        score += CreateLongTrendIndicator(
            indicators,
            price,
            sma200);

        score += CreateRsiIndicator(
            indicators,
            rsi);

        score += CreateMacdIndicator(
            indicators,
            macd);

        score = Math.Clamp(score, 0, 100);

        var (signalCode, signalLabel) =
            GetSignal(score);

        var confidence =
            CalculateConfidence(
                closes.Count,
                indicators.Count(indicator =>
                    indicator.Value.HasValue));

        return new MarketTechnicalAnalysisDto(
            marketAssetId,
            history.QuoteCurrency,
            true,
            signalCode,
            signalLabel,
            score,
            confidence,
            CreateSummary(
                price,
                sma50,
                sma200,
                rsi,
                macd),
            closes.Count,
            DateTime.UtcNow,
            indicators);
    }

    private static int CreateMovingAverageIndicator(
        ICollection<MarketTechnicalIndicatorDto> indicators,
        decimal price,
        decimal? sma20,
        decimal? sma50)
    {
        if (!sma20.HasValue)
        {
            return 0;
        }

        var score =
            price >= sma20.Value ? 7 : -7;

        var status =
            price >= sma20.Value
                ? "Olumlu"
                : "Olumsuz";

        var description =
            price >= sma20.Value
                ? "Fiyat SMA20 üzerinde; kısa vadeli " +
                  "trend yukarı yönlü."
                : "Fiyat SMA20 altında; kısa vadeli " +
                  "trend zayıf.";

        if (sma50.HasValue)
        {
            score +=
                price >= sma50.Value ? 9 : -9;

            score +=
                sma20.Value >= sma50.Value ? 10 : -10;

            description +=
                sma20.Value >= sma50.Value
                    ? " SMA20, SMA50 üzerinde."
                    : " SMA20, SMA50 altında.";
        }

        indicators.Add(
            new MarketTechnicalIndicatorDto(
                "moving-averages",
                "Hareketli Ortalamalar",
                sma50 ?? sma20,
                status,
                description));

        return score;
    }

    private static int CreateLongTrendIndicator(
        ICollection<MarketTechnicalIndicatorDto> indicators,
        decimal price,
        decimal? sma200)
    {
        if (!sma200.HasValue)
        {
            indicators.Add(
                new MarketTechnicalIndicatorDto(
                    "sma-200",
                    "Uzun Vadeli Trend",
                    null,
                    "Veri yetersiz",
                    "SMA200 için 200 fiyat noktası gerekiyor."));

            return 0;
        }

        var isPositive =
            price >= sma200.Value;

        indicators.Add(
            new MarketTechnicalIndicatorDto(
                "sma-200",
                "Uzun Vadeli Trend",
                sma200.Value,
                isPositive ? "Olumlu" : "Olumsuz",
                isPositive
                    ? "Fiyat SMA200 üzerinde; uzun vadeli " +
                      "ana trend pozitif."
                    : "Fiyat SMA200 altında; uzun vadeli " +
                      "ana trend baskı altında."));

        return isPositive ? 12 : -12;
    }

    private static int CreateRsiIndicator(
        ICollection<MarketTechnicalIndicatorDto> indicators,
        decimal? rsi)
    {
        if (!rsi.HasValue)
        {
            return 0;
        }

        string status;
        string description;
        int score;

        if (rsi.Value >= 70)
        {
            status = "Aşırı alım";
            description =
                "RSI 70 üzerinde; yükseliş güçlü ancak " +
                "kısa vadeli düzeltme riski artmış olabilir.";
            score = -8;
        }
        else if (rsi.Value <= 30)
        {
            status = "Aşırı satım";
            description =
                "RSI 30 altında; satış baskısı yüksek, " +
                "tepki ihtimali oluşabilir.";
            score = 8;
        }
        else if (rsi.Value >= 50)
        {
            status = "Olumlu";
            description =
                "RSI 50 üzerinde; momentum alıcılar lehine.";
            score = 6;
        }
        else
        {
            status = "Zayıf";
            description =
                "RSI 50 altında; momentum henüz zayıf.";
            score = -4;
        }

        indicators.Add(
            new MarketTechnicalIndicatorDto(
                "rsi-14",
                "RSI (14)",
                Math.Round(rsi.Value, 2),
                status,
                description));

        return score;
    }

    private static int CreateMacdIndicator(
        ICollection<MarketTechnicalIndicatorDto> indicators,
        MacdValue? macd)
    {
        if (macd is null)
        {
            return 0;
        }

        var isPositive =
            macd.Histogram >= 0;

        indicators.Add(
            new MarketTechnicalIndicatorDto(
                "macd",
                "MACD (12, 26, 9)",
                Math.Round(macd.Histogram, 4),
                isPositive ? "Olumlu" : "Olumsuz",
                isPositive
                    ? "MACD, sinyal çizgisinin üzerinde; " +
                      "momentum pozitif."
                    : "MACD, sinyal çizgisinin altında; " +
                      "momentum negatif."));

        return isPositive ? 12 : -12;
    }

    private static decimal? CalculateSma(
        IReadOnlyList<decimal> values,
        int period)
    {
        if (values.Count < period)
        {
            return null;
        }

        return values
            .Skip(values.Count - period)
            .Average();
    }

    private static decimal? CalculateRsi(
        IReadOnlyList<decimal> values,
        int period)
    {
        if (values.Count <= period)
        {
            return null;
        }

        decimal averageGain = 0;
        decimal averageLoss = 0;

        for (var index = 1;
             index <= period;
             index++)
        {
            var change =
                values[index] - values[index - 1];

            if (change >= 0)
            {
                averageGain += change;
            }
            else
            {
                averageLoss -= change;
            }
        }

        averageGain /= period;
        averageLoss /= period;

        for (var index = period + 1;
             index < values.Count;
             index++)
        {
            var change =
                values[index] - values[index - 1];

            var gain =
                Math.Max(change, 0);

            var loss =
                Math.Max(-change, 0);

            averageGain =
                ((averageGain * (period - 1)) + gain)
                / period;

            averageLoss =
                ((averageLoss * (period - 1)) + loss)
                / period;
        }

        if (averageLoss == 0)
        {
            return 100;
        }

        var relativeStrength =
            averageGain / averageLoss;

        return 100m -
               (100m / (1m + relativeStrength));
    }

    private static MacdValue? CalculateMacd(
        IReadOnlyList<decimal> values)
    {
        if (values.Count <
            MacdSlowPeriod + MacdSignalPeriod)
        {
            return null;
        }

        var fastEma =
            CalculateEma(values, MacdFastPeriod);

        var slowEma =
            CalculateEma(values, MacdSlowPeriod);

        var macdValues =
            new List<decimal>();

        for (var index = 0;
             index < values.Count;
             index++)
        {
            if (fastEma[index].HasValue &&
                slowEma[index].HasValue)
            {
                macdValues.Add(
                    fastEma[index]!.Value -
                    slowEma[index]!.Value);
            }
        }

        var signalValues =
            CalculateEma(
                macdValues,
                MacdSignalPeriod);

        var macd = macdValues[^1];
        var signal = signalValues[^1];

        if (!signal.HasValue)
        {
            return null;
        }

        return new MacdValue(
            macd,
            signal.Value,
            macd - signal.Value);
    }

    private static IReadOnlyList<decimal?> CalculateEma(
        IReadOnlyList<decimal> values,
        int period)
    {
        var result =
            Enumerable.Repeat<decimal?>(
                    null,
                    values.Count)
                .ToArray();

        if (values.Count < period)
        {
            return result;
        }

        var previousEma =
            values.Take(period).Average();

        result[period - 1] = previousEma;

        var multiplier =
            2m / (period + 1m);

        for (var index = period;
             index < values.Count;
             index++)
        {
            previousEma =
                ((values[index] - previousEma)
                    * multiplier)
                + previousEma;

            result[index] = previousEma;
        }

        return result;
    }

    private static int CalculateConfidence(
        int dataPointCount,
        int availableIndicatorCount)
    {
        var dataScore =
            Math.Min(dataPointCount / 200d, 1d)
            * 70d;

        var indicatorScore =
            Math.Min(availableIndicatorCount / 4d, 1d)
            * 30d;

        return Math.Clamp(
            (int)Math.Round(
                dataScore + indicatorScore),
            20,
            100);
    }

    private static (string Code, string Label)
        GetSignal(int score)
    {
        return score switch
        {
            >= 75 => ("strong-buy", "Güçlü Al"),
            >= 60 => ("buy", "Al"),
            >= 40 => ("hold", "Tut"),
            >= 25 => ("sell", "Sat"),
            _ => ("strong-sell", "Güçlü Sat")
        };
    }

    private static string CreateSummary(
        decimal price,
        decimal? sma50,
        decimal? sma200,
        decimal? rsi,
        MacdValue? macd)
    {
        var trend =
            sma200.HasValue
                ? price >= sma200.Value
                    ? "Uzun vadeli trend olumlu."
                    : "Uzun vadeli trend baskı altında."
                : sma50.HasValue
                    ? price >= sma50.Value
                        ? "Orta vadeli trend olumlu."
                        : "Orta vadeli trend zayıf."
                    : "Trend için veri sınırlı.";

        var momentum =
            macd is not null
                ? macd.Histogram >= 0
                    ? " MACD momentumu destekliyor."
                    : " MACD momentumu zayıflatıyor."
                : "";

        var rsiText =
            rsi switch
            {
                >= 70 => " RSI aşırı alım bölgesinde.",
                <= 30 => " RSI aşırı satım bölgesinde.",
                _ => ""
            };

        return trend + momentum + rsiText;
    }

    private static MarketTechnicalAnalysisDto
        CreateUnavailableResult(
            int marketAssetId,
            string quoteCurrency,
            int dataPointCount,
            string message)
    {
        return new MarketTechnicalAnalysisDto(
            marketAssetId,
            quoteCurrency,
            false,
            "unavailable",
            "Hesaplanamadı",
            50,
            0,
            message,
            dataPointCount,
            DateTime.UtcNow,
            []);
    }

    private sealed record MacdValue(
        decimal Macd,
        decimal Signal,
        decimal Histogram);
}
