namespace MiniMola.Application.Markets;

public sealed record MarketHistoryPointDto(
    long Time,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal Close,
    decimal? Volume);
