namespace MiniMola.Application.Markets;

public sealed record FundEstimateDto(
    int AssetId,
    bool IsSupported,
    bool IsAvailable,
    string QuoteCurrency,
    decimal? BasePrice,
    DateTime? BasePriceObservedAtUtc,
    decimal? EstimatedChangePercent,
    decimal? EstimatedPrice,
    decimal CoveragePercent,
    string ConfidenceCode,
    string ConfidenceLabel,
    DateTime CalculatedAtUtc,
    DateOnly? DistributionDate,
    string Methodology,
    string? Message,
    IReadOnlyList<FundEstimateContributionDto> Contributions)
{
    public string ModelVersion { get; init; } = "allocation-proxy-v1";
}

public sealed record FundEstimateContributionDto(
    string CategoryCode,
    string CategoryName,
    decimal WeightPercent,
    string? ProxySymbol,
    decimal? ProxyChangePercent,
    decimal? ContributionPercent,
    DateTime? ProxyObservedAtUtc,
    bool IsCovered);
