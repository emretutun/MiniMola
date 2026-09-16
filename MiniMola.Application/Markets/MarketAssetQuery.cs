using MiniMola.Domain.Enums;

namespace MiniMola.Application.Markets;

public sealed record MarketAssetQuery(
    MarketAssetType? AssetType = null,
    string? Search = null,
    bool FavoritesOnly = false,
    int Page = 1,
    int PageSize = 30);