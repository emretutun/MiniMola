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
    string AssetKey,
    string Nickname,
    string? ColorVariantKey,
    string Rarity,
    float BaseSpeed,
    float DisplayScale);

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