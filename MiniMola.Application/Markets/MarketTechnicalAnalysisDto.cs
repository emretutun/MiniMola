namespace MiniMola.Application.Markets;

public sealed record MarketTechnicalAnalysisDto(
    int AssetId,
    string QuoteCurrency,
    bool IsAvailable,
    string SignalCode,
    string SignalLabel,
    int Score,
    int Confidence,
    string Summary,
    int DataPointCount,
    DateTime CalculatedAtUtc,
    IReadOnlyList<MarketTechnicalIndicatorDto> Indicators);
