using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class MarketWatchlistService(
    ApplicationDbContext dbContext)
    : IMarketWatchlistService
{
    public async Task<
        PagedResult<MarketAssetListItemDto>>
        GetAssetsAsync(
            string identityUserId,
            MarketAssetQuery query,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        ArgumentNullException.ThrowIfNull(query);

        var page =
            Math.Max(query.Page, 1);

        var pageSize =
            Math.Clamp(query.PageSize, 1, 50);

        var userProfileId =
            await GetUserProfileIdAsync(
                identityUserId,
                cancellationToken);

        if (!userProfileId.HasValue)
        {
            return new PagedResult<
                MarketAssetListItemDto>(
                    [],
                    1,
                    pageSize,
                    0);
        }

        var favoriteRows =
            await dbContext.UserFavoriteAssets
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId
                    == userProfileId.Value)
                .Select(x => new
                {
                    x.MarketAssetId,
                    x.SortOrder
                })
                .ToListAsync(cancellationToken);

        var favoriteOrderLookup =
            favoriteRows.ToDictionary(
                x => x.MarketAssetId,
                x => x.SortOrder);

        var favoriteAssetIds =
            favoriteRows
                .Select(x => x.MarketAssetId)
                .ToList();

        var assetQuery =
            dbContext.MarketAssets
                .AsNoTracking()
                .Where(x => x.IsActive);

        if (query.AssetType.HasValue)
        {
            assetQuery = assetQuery.Where(x =>
                x.AssetType == query.AssetType.Value);
        }

        if (!string.IsNullOrWhiteSpace(
            query.Search))
        {
            var normalizedSearch =
                query.Search.Trim();

            assetQuery = assetQuery.Where(x =>
                x.Symbol.Contains(
                    normalizedSearch)
                || x.Name.Contains(
                    normalizedSearch));
        }

        if (query.FavoritesOnly)
        {
            assetQuery = assetQuery.Where(x =>
                favoriteAssetIds.Contains(x.Id));
        }
        else if (!query.AssetType.HasValue
                 && string.IsNullOrWhiteSpace(
                     query.Search))
        {
            assetQuery = assetQuery.Where(x =>
                x.IsFeatured
                || favoriteAssetIds.Contains(x.Id));
        }

        var totalCount =
            await assetQuery.CountAsync(
                cancellationToken);

        var totalPages =
            totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)pageSize);

        page = totalPages == 0
            ? 1
            : Math.Min(page, totalPages);

        var favoriteQuery =
            dbContext.UserFavoriteAssets
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId ==
                    userProfileId.Value);

        var orderedAssetQuery =
            from asset in assetQuery
            join favorite in favoriteQuery
                on asset.Id equals favorite.MarketAssetId
                into favoriteGroup
            from favorite in favoriteGroup.DefaultIfEmpty()
            orderby
                favorite == null ? 1 : 0,
                favorite.SortOrder,
                asset.IsFeatured descending,
                asset.Symbol,
                asset.Id
            select asset;

        var assets =
            await orderedAssetQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

        var assetIds =
            assets
                .Select(x => x.Id)
                .ToList();

        var latestPriceLookup =
            new Dictionary<int, MarketPriceSnapshot>();

        if (assetIds.Count > 0)
        {
            var latestPrices =
                await dbContext.MarketPriceSnapshots
                    .AsNoTracking()
                    .Where(x =>
                        assetIds.Contains(
                            x.MarketAssetId))
                    .GroupBy(x => x.MarketAssetId)
                    .Select(group =>
                        group
                            .OrderByDescending(x =>
                                x.ObservedAtUtc)
                            .ThenByDescending(x => x.Id)
                            .First())
                    .ToListAsync(cancellationToken);

            latestPriceLookup =
                latestPrices.ToDictionary(
                    x => x.MarketAssetId);
        }

        var items =
            assets
            .Select(asset =>
            {
                latestPriceLookup.TryGetValue(
                    asset.Id,
                    out var latestPrice);

                var isFavorite =
                    favoriteOrderLookup.ContainsKey(
                        asset.Id);

                return new MarketAssetListItemDto(
                    asset.Id,
                    asset.Symbol,
                    asset.Name,
                    asset.AssetType,
                    asset.MarketCode,
                    asset.QuoteCurrency,
                    latestPrice?.Price,
                    latestPrice?.DailyChangePercent,
                    latestPrice?.PriceKind,
                    latestPrice?.Source,
                    latestPrice?.ObservedAtUtc,
                    isFavorite);
            })
            .ToList();

        return new PagedResult<
            MarketAssetListItemDto>(
                items,
                page,
                pageSize,
                totalCount);
    }

    public async Task<bool> AddFavoriteAsync(
        string identityUserId,
        int marketAssetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        if (marketAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marketAssetId));
        }

        var userProfileId =
            await GetUserProfileIdAsync(
                identityUserId,
                cancellationToken);

        if (!userProfileId.HasValue)
        {
            return false;
        }

        var assetExists =
            await dbContext.MarketAssets
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.Id == marketAssetId
                        && x.IsActive,
                    cancellationToken);

        if (!assetExists)
        {
            return false;
        }

        var alreadyFavorite =
            await dbContext.UserFavoriteAssets
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserProfileId
                        == userProfileId.Value
                        && x.MarketAssetId
                        == marketAssetId,
                    cancellationToken);

        if (alreadyFavorite)
        {
            return true;
        }

        var lastSortOrder =
            await dbContext.UserFavoriteAssets
                .Where(x =>
                    x.UserProfileId
                    == userProfileId.Value)
                .MaxAsync(
                    x => (int?)x.SortOrder,
                    cancellationToken);

        dbContext.UserFavoriteAssets.Add(
            new UserFavoriteAsset
            {
                UserProfileId =
                    userProfileId.Value,

                MarketAssetId =
                    marketAssetId,

                SortOrder =
                    (lastSortOrder ?? -1) + 1
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(
        string identityUserId,
        int marketAssetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        if (marketAssetId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(marketAssetId));
        }

        var userProfileId =
            await GetUserProfileIdAsync(
                identityUserId,
                cancellationToken);

        if (!userProfileId.HasValue)
        {
            return false;
        }

        var favorite =
            await dbContext.UserFavoriteAssets
                .SingleOrDefaultAsync(
                    x =>
                        x.UserProfileId
                        == userProfileId.Value
                        && x.MarketAssetId
                        == marketAssetId,
                    cancellationToken);

        if (favorite is null)
        {
            return false;
        }

        dbContext.UserFavoriteAssets.Remove(favorite);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return true;
    }

    private async Task<int?> GetUserProfileIdAsync(
        string identityUserId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x =>
                x.IdentityUserId == identityUserId)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
