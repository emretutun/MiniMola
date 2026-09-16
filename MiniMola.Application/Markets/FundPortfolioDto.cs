namespace MiniMola.Application.Markets;

public sealed record FundPortfolioDto(
    int AssetId,
    bool IsSupported,
    bool IsAvailable,
    DateOnly? ReportDate,
    DateTime? PublishedAtUtc,
    int? ReportAgeDays,
    decimal ParsedWeightPercent,
    decimal MatchedWeightPercent,
    string? NotificationUrl,
    string? DocumentUrl,
    string? Message,
    IReadOnlyList<FundPortfolioHoldingDto> Holdings);

public sealed record FundPortfolioHoldingDto(
    string Symbol,
    string Name,
    decimal WeightPercent,
    int? MarketAssetId);
