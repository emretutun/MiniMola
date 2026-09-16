using MiniMola.Application.Markets;

namespace MiniMola.Infrastructure.Services;

public sealed class MarketAssetCatalogSyncService(
    IEnumerable<IMarketAssetCatalogProvider> providers)
    : IMarketAssetCatalogSyncService
{
    public async Task SyncCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var provider in providers)
        {
            await provider.SyncAsync(cancellationToken);
        }
    }
}
