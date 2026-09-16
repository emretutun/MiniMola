namespace MiniMola.Application.Markets;

public interface IMarketAssetCatalogSyncService
{
    Task SyncCatalogsAsync(
        CancellationToken cancellationToken = default);
}
