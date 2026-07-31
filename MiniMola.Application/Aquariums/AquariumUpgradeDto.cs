using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public sealed record AquariumUpgradeDto(
    int Level,
    int Capacity,
    int? NextLevel,
    int? NextCapacity,
    int? UpgradeCost,
    int PointBalance,
    bool CanUpgrade,
    string? LockedReason);