namespace MiniMola.Application.Markets;

public interface IMarketDataHealthService
{
    Task<IReadOnlyList<MarketDataHealthDto>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<string?> RetryAsync(string userId, int assetId, CancellationToken cancellationToken = default);
}

public sealed record MarketDataHealthDto(int AssetId, string Symbol, string Name, string Provider,
    string PriceStatus, DateTime? ObservedAtUtc, DateTime? StoredAtUtc,
    DateOnly? ReportDate, decimal? MatchedWeightPercent, string ReportStatus, string? LastReportMessage,
    decimal? LastEstimateCoverage, DateTime? LastEstimateAtUtc);
