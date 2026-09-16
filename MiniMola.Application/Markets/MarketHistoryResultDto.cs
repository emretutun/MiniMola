using MiniMola.Domain.Enums;

namespace MiniMola.Application.Markets;

public sealed record MarketHistoryResultDto(
    int AssetId,
    string Symbol,
    string Name,
    MarketAssetType AssetType,
    string MarketCode,
    string QuoteCurrency,
    decimal? Price,
    decimal? DailyChangePercent,
    MarketPriceKind? PriceKind,
    string? PriceSource,
    DateTime? PriceObservedAtUtc,
    string Range,
    string Interval,
    string HistorySource,
    bool IsSupported,
    string? Message,
    IReadOnlyList<MarketHistoryPointDto> Points);
