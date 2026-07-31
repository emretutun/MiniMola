using System;
using System.Collections.Generic;
using System.Text;

using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class Aquarium : BaseEntity
{
    public int UserProfileId { get; set; }

    public string Name { get; set; } = "Akvaryumum";

    public string ThemeKey { get; set; } = "starter-ocean";

    public int Level { get; set; } = 1;

    public int Capacity { get; set; } = 5;

    public UserProfile UserProfile { get; set; } = null!;
}