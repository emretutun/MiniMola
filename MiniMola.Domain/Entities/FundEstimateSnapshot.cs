using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class FundEstimateSnapshot : BaseEntity
{
    public int MarketAssetId { get; set; }

    public DateOnly TargetDate { get; set; }

    public FundEstimateKind Kind { get; set; }

    public decimal BasePrice { get; set; }

    public DateTime BasePriceObservedAtUtc { get; set; }

    public decimal EstimatedChangePercent { get; set; }

    public decimal EstimatedPrice { get; set; }

    public decimal CoveragePercent { get; set; }

    public string ConfidenceCode { get; set; } = string.Empty;

    public DateOnly? DistributionDate { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public string ModelVersion { get; set; } = string.Empty;

    public decimal? ActualPrice { get; set; }

    public decimal? ActualChangePercent { get; set; }

    public DateTime? ActualObservedAtUtc { get; set; }

    public decimal? AbsoluteErrorPercent { get; set; }

    public DateTime? EvaluatedAtUtc { get; set; }

    public MarketAsset MarketAsset { get; set; } = null!;
}
