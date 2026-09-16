namespace MiniMola.Application.Markets;

public sealed record FundEstimateHistoryDto(
    int AssetId,
    int EvaluatedCount,
    decimal? MeanAbsoluteErrorPercent,
    decimal? WithinOnePercentRate,
    IReadOnlyList<FundEstimateHistoryItemDto> Items);

public sealed record FundEstimateHistoryItemDto(
    DateOnly TargetDate,
    decimal EstimatedChangePercent,
    decimal EstimatedPrice,
    decimal CoveragePercent,
    string ConfidenceCode,
    DateTime CalculatedAtUtc,
    decimal? ActualChangePercent,
    decimal? ActualPrice,
    DateTime? ActualObservedAtUtc,
    decimal? AbsoluteErrorPercent);
