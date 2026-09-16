namespace MiniMola.Application.Markets;

public interface IMarketPriceRefreshService
{
    Task RefreshTrackedAssetsAsync(
        CancellationToken cancellationToken = default);

    Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default);
}
