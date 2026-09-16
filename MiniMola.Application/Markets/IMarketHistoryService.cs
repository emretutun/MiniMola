namespace MiniMola.Application.Markets;

public interface IMarketHistoryService
{
    Task<MarketHistoryResultDto?> GetHistoryAsync(
        int marketAssetId,
        string? range,
        CancellationToken cancellationToken = default);
}
