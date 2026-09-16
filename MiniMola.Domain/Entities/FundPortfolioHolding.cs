using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class FundPortfolioHolding : BaseEntity
{
    public int FundPortfolioReportId { get; set; }

    public string SecuritySymbol { get; set; } = string.Empty;

    public string SecurityName { get; set; } = string.Empty;

    public decimal WeightPercent { get; set; }

    public int? MatchedMarketAssetId { get; set; }

    public FundPortfolioReport FundPortfolioReport { get; set; }
        = null!;

    public MarketAsset? MatchedMarketAsset { get; set; }
}
