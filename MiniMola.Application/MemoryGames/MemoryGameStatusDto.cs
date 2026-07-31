using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.MemoryGames;

public sealed record MemoryGameStatusDto(
    DateOnly GameDate,
    int PairCount,
    int RewardPoints,
    int PointBalance,
    bool RewardClaimed,
    string? Message);