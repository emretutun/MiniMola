using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Shop;

public sealed record DecorationShopDto(
    int PointBalance,
    int AquariumLevel,
    int PlacedDecorationCount,
    IReadOnlyList<DecorationShopItemDto> Items);

public sealed record DecorationShopItemDto(
    int Id,
    string Name,
    string Description,
    string AssetKey,
    string Category,
    int Price,
    int RequiredAquariumLevel,
    float DisplayScale,
    int OwnedCount,
    bool CanPurchase,
    string? LockedReason);

public sealed record PurchaseDecorationResultDto(
    bool Success,
    string Message,
    int PointBalance,
    int? UserDecorationId,
    bool PlacedInAquarium);