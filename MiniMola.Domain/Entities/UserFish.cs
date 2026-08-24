using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class UserFish : BaseEntity
{
    public int UserProfileId { get; set; }

    public int FishSpeciesId { get; set; }

    public int? AquariumId { get; set; }

    public string Nickname { get; set; } = string.Empty;

    public string? ColorVariantKey { get; set; }

    public DateTime AcquiredAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastFedAtUtc { get; set; }

    public int TotalFeedings { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public FishSpecies FishSpecies { get; set; } = null!;

    public Aquarium? Aquarium { get; set; }
}