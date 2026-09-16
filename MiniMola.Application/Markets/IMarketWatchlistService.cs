using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Enums;

namespace MiniMola.Application.Markets;

public interface IMarketWatchlistService
{
    Task<PagedResult<MarketAssetListItemDto>>
        GetAssetsAsync(
            string identityUserId,
            MarketAssetQuery query,
            CancellationToken cancellationToken = default);

    Task<bool> AddFavoriteAsync(
        string identityUserId,
        int marketAssetId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFavoriteAsync(
        string identityUserId,
        int marketAssetId,
        CancellationToken cancellationToken = default);
}