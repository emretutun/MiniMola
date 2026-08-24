using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public sealed record AquariumDetailsDto(
    int Id,
    string Name,
    string ThemeKey,
    int Level,
    int Capacity,
    string UserDisplayName,
    int PointBalance,
    IReadOnlyList<AquariumFishDto> Fish,
    IReadOnlyList<AquariumDecorationDto> Decorations);

public sealed record AquariumFishDto(
    int Id,
    string SpeciesName,
    string SpeciesDescription,
    string AssetKey,
    string Nickname,
    DateTime AcquiredAtUtc,
    string? ColorVariantKey,
    string Rarity,
    float BaseSpeed,
    float DisplayScale,
    DateTime? LastFedAtUtc,
    int TotalFeedings,
    int HappinessPercent,
    string CareStatus,
    bool CanFeed,
    DateTime? NextFeedAtUtc);

public sealed record AquariumDecorationDto(
    int Id,
    string Name,
    string AssetKey,
    string Category,
    float PositionX,
    float PositionY,
    int ZIndex,
    float Rotation,
    float Scale);