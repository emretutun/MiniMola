using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.MemoryGames;

public sealed record MemoryGameResultDto(
    bool Success,
    string Message,
    int PointsAwarded,
    int PointBalance,
    bool RewardClaimed);