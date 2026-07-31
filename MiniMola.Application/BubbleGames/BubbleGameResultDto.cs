using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.BubbleGames;

public sealed record BubbleGameResultDto(
    bool Success,
    string Message,
    int PointsAwarded,
    int PointBalance,
    bool RewardClaimed);