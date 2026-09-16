using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class FundPortfolioReport : BaseEntity
{
    public int FundMarketAssetId { get; set; }

    public DateOnly ReportDate { get; set; }

    public DateTime PublishedAtUtc { get; set; }

    public long KapNotificationId { get; set; }

    public string DocumentObjectId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string DocumentUrl { get; set; } = string.Empty;

    public string NotificationUrl { get; set; } = string.Empty;

    public string ParserVersion { get; set; } = string.Empty;

    public decimal ParsedWeightPercent { get; set; }

    public decimal MatchedWeightPercent { get; set; }

    public MarketAsset FundMarketAsset { get; set; } = null!;

    public ICollection<FundPortfolioHolding> Holdings { get; set; }
        = new List<FundPortfolioHolding>();
}
