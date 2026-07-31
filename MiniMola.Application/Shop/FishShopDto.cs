namespace MiniMola.Application.Shop;

public sealed record FishShopDto(
    int PointBalance,
    int AquariumLevel,
    int AquariumCapacity,
    int PlacedFishCount,
    IReadOnlyList<FishShopItemDto> Items);

public sealed record FishShopItemDto(
    int Id,
    string Name,
    string Description,
    string AssetKey,
    int Price,
    string Rarity,
    int RequiredAquariumLevel,
    int OwnedCount,
    bool CanPurchase,
    string? LockedReason);

public sealed record PurchaseFishResultDto(
    bool Success,
    string Message,
    int PointBalance,
    int? UserFishId,
    bool PlacedInAquarium);