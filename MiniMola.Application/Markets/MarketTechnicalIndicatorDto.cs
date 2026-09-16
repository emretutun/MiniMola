namespace MiniMola.Application.Markets;

public sealed record MarketTechnicalIndicatorDto(
    string Code,
    string Name,
    decimal? Value,
    string Status,
    string Description);
