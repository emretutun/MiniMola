using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class FishSpecies : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string AssetKey { get; set; } = string.Empty;

    public int Price { get; set; }

    public FishRarity Rarity { get; set; } = FishRarity.Common;

    public float BaseSpeed { get; set; } = 1.0f;

    public float DisplayScale { get; set; } = 1.0f;

    public int RequiredAquariumLevel { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}