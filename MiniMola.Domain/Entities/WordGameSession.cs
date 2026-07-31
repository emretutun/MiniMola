using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;
using MiniMola.Domain.Enums;

namespace MiniMola.Domain.Entities;

public sealed class WordGameSession : BaseEntity
{
    public int UserProfileId { get; set; }

    public int DailyWordPuzzleId { get; set; }

    public WordGameStatus Status { get; set; }
        = WordGameStatus.InProgress;

    public int AttemptCount { get; set; }

    public bool RewardGranted { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public UserProfile UserProfile { get; set; } = null!;

    public DailyWordPuzzle DailyWordPuzzle { get; set; } = null!;
}