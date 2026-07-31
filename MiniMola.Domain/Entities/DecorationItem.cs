using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class DecorationItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string AssetKey { get; set; } = string.Empty;

    public DecorationCategory Category { get; set; }

    public int Price { get; set; }

    public int RequiredAquariumLevel { get; set; } = 1;

    public float DisplayScale { get; set; } = 1.0f;

    public bool IsActive { get; set; } = true;
}