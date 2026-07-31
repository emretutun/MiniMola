using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.BubbleGames;

public sealed record BubbleGameStatusDto(
    DateOnly GameDate,
    int DurationSeconds,
    int TargetScore,
    int RewardPoints,
    int PointBalance,
    bool RewardClaimed,
    string? Message);