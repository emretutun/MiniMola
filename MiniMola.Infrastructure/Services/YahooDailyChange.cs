using System.Text.Json;

namespace MiniMola.Infrastructure.Services;

public static class YahooDailyChange
{
    // chartPreviousClose is the start of the requested range, not yesterday.
    public static decimal? Calculate(JsonElement result, decimal price, long quoteTime)
    {
        var meta = result.GetProperty("meta");
        decimal? previous = null;
        if (meta.TryGetProperty("previousClose", out var close)
            && close.ValueKind == JsonValueKind.Number
            && close.TryGetDecimal(out var value) && value > 0)
            previous = value;

        if (previous is null
            && result.TryGetProperty("timestamp", out var timestamps)
            && timestamps.ValueKind == JsonValueKind.Array
            && result.TryGetProperty("indicators", out var indicators)
            && indicators.TryGetProperty("quote", out var quotes)
            && quotes.ValueKind == JsonValueKind.Array && quotes.GetArrayLength() > 0
            && quotes[0].TryGetProperty("close", out var closes)
            && closes.ValueKind == JsonValueKind.Array)
        {
            var offset = meta.TryGetProperty("gmtoffset", out var offsetElement)
                && offsetElement.TryGetInt32(out var seconds) ? seconds : 0;
            var day = DateTimeOffset.FromUnixTimeSeconds(quoteTime).AddSeconds(offset).Date;
            long latest = 0;
            for (var i = 0; i < Math.Min(timestamps.GetArrayLength(), closes.GetArrayLength()); i++)
            {
                if (timestamps[i].ValueKind != JsonValueKind.Number
                    || !timestamps[i].TryGetInt64(out var time) || time <= latest
                    || time > 253402214399 || time <= 0) continue;
                if (DateTimeOffset.FromUnixTimeSeconds(time).AddSeconds(offset).Date >= day) continue;
                latest = time;
                previous = closes[i].ValueKind == JsonValueKind.Number
                    && closes[i].TryGetDecimal(out value) && value > 0 ? value : null;
            }
        }
        if (previous is > 0) return (price / previous.Value - 1m) * 100m;
        // Some BIST responses omit yesterday's bar but expose the daily return in meta.
        return meta.TryGetProperty("regularMarketChangePercent", out var change)
            && change.ValueKind == JsonValueKind.Number && change.TryGetDecimal(out var percent)
            && percent >= -100m ? percent : null;
    }
}
