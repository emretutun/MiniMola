using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class UserDecoration : BaseEntity
{
    public int UserProfileId { get; set; }

    public int DecorationItemId { get; set; }

    public int? AquariumId { get; set; }

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public int ZIndex { get; set; }

    public float Rotation { get; set; }

    public float Scale { get; set; } = 1.0f;

    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfile UserProfile { get; set; } = null!;

    public DecorationItem DecorationItem { get; set; } = null!;

    public Aquarium? Aquarium { get; set; }
}