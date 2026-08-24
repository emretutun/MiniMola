using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.Aquariums;

public sealed record FeedFishResultDto(
    bool Success,
    string Message,
    int UserFishId,
    DateTime? LastFedAtUtc,
    int TotalFeedings,
    int HappinessPercent,
    string CareStatus,
    bool CanFeed,
    DateTime? NextFeedAtUtc);