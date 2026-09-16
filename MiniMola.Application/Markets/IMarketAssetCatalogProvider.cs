namespace MiniMola.Application.Markets;

public interface IMarketAssetCatalogProvider
{
    Task SyncAsync(
        CancellationToken cancellationToken = default);
}
