using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Services;

public static class FundEstimateTrackingPolicy
{
    // Application capture window, not an exchange calendar. Half days are not inferred.
    public static bool IsClosingWindow(DateTime utc)
    {
        var local = utc.AddHours(3);
        return local.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
            && TimeOnly.FromDateTime(local) >= new TimeOnly(18, 45)
            && TimeOnly.FromDateTime(local) <= new TimeOnly(19, 30);
    }

    public static bool IsClosingQuote(DateTime quoteUtc, DateTime nowUtc) =>
        quoteUtc <= nowUtc && quoteUtc.AddHours(3).Date == nowUtc.AddHours(3).Date
        && TimeOnly.FromDateTime(quoteUtc.AddHours(3)) >= new TimeOnly(18, 8);

    public static FundEstimateKind? GetKind(FundEstimateDto result)
    {
        var local = result.CalculatedAtUtc.AddHours(3);
        if (!result.IsAvailable || result.BasePrice is not > 0
            || result.EstimatedPrice is not > 0 || !result.EstimatedChangePercent.HasValue
            || result.BasePriceObservedAtUtc?.Date != local.Date
            || local.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) return null;

        if (TimeOnly.FromDateTime(local) < new TimeOnly(18, 10)) return FundEstimateKind.Intraday;
        if (!IsClosingWindow(result.CalculatedAtUtc) || result.ModelVersion != "kap-holdings-v2") return null;
        var covered = result.Contributions.Where(x => x.IsCovered).ToList();
        return covered.Sum(x => x.WeightPercent) >= 60m
            && covered.All(x => x.ProxyObservedAtUtc is DateTime date
                && IsClosingQuote(date, result.CalculatedAtUtc)) ? FundEstimateKind.Closing : null;
    }

    public static bool CanReplace(FundEstimateSnapshot existing, FundEstimateDto result) =>
        existing.Kind == FundEstimateKind.Intraday && existing.EvaluatedAtUtc == null
        && result.CalculatedAtUtc > existing.CalculatedAtUtc;

    public static bool TryEvaluate(FundEstimateSnapshot estimate, MarketPriceSnapshot actual, DateTime nowUtc)
    {
        if (estimate.Kind == FundEstimateKind.Legacy || estimate.EvaluatedAtUtc != null
            || estimate.BasePrice <= 0 || actual.Price <= 0
            || actual.MarketAssetId != estimate.MarketAssetId
            || actual.PriceKind != MarketPriceKind.Official || actual.Source != "TEFAS / BEFAS"
            || DateOnly.FromDateTime(actual.ObservedAtUtc) != estimate.TargetDate
            || actual.ObservedAtUtc.Date <= estimate.BasePriceObservedAtUtc.Date
            || actual.CreatedAtUtc <= estimate.CalculatedAtUtc || actual.CreatedAtUtc > nowUtc) return false;
        estimate.ActualPrice = actual.Price;
        estimate.ActualChangePercent = decimal.Round((actual.Price / estimate.BasePrice - 1m) * 100m, 6);
        estimate.ActualObservedAtUtc = actual.ObservedAtUtc;
        estimate.AbsoluteErrorPercent = decimal.Round(Math.Abs(estimate.EstimatedChangePercent - estimate.ActualChangePercent.Value), 6);
        estimate.EvaluatedAtUtc = nowUtc;
        estimate.UpdatedAtUtc = nowUtc;
        return true;
    }
}
