using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Markets;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class MarketPriceRefreshService(
    ApplicationDbContext dbContext,
    IEnumerable<IMarketPriceProvider> providers)
    : IMarketPriceRefreshService
{
    public async Task RefreshTrackedAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        var favoriteAssetIds =
            dbContext.UserFavoriteAssets
                .Select(favorite =>
                    favorite.MarketAssetId);

        var trackedAssetIds =
            await dbContext.MarketAssets
                .AsNoTracking()
                .Where(asset =>
                    asset.IsActive
                    && asset.DataProviderCode != null
                    && asset.ProviderSymbol != null
                    && (asset.IsFeatured
                        || favoriteAssetIds.Contains(
                            asset.Id)))
                .Select(asset => asset.Id)
                .ToListAsync(cancellationToken);

        // Loaded KAP positions also need prices before the fund detail is opened.
        var reportCutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).AddDays(-45);
        var holdingIds = await dbContext.FundPortfolioHoldings.AsNoTracking()
            .Where(x => x.MatchedMarketAssetId != null
                && x.FundPortfolioReport.ReportDate >= reportCutoff)
            .Select(x => x.MatchedMarketAssetId!.Value).Distinct().ToListAsync(cancellationToken);
        trackedAssetIds = trackedAssetIds.Concat(holdingIds).Distinct().ToList();

        await RefreshStalePricesAsync(
            trackedAssetIds,
            cancellationToken);
    }

    public async Task RefreshStalePricesAsync(
        IReadOnlyCollection<int> marketAssetIds,
        CancellationToken cancellationToken = default)
    {
        if (marketAssetIds.Count == 0)
        {
            return;
        }

        foreach (var provider in providers)
        {
            await provider.RefreshStalePricesAsync(
                marketAssetIds,
                cancellationToken);
        }
    }
}
