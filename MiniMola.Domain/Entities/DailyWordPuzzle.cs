using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class DailyWordPuzzle : BaseEntity
{
    public DateOnly PuzzleDate { get; set; }

    public string Word { get; set; } = string.Empty;

    public string Hint { get; set; } = string.Empty;

    public int RewardPoints { get; set; } = 30;

    public int MaxAttempts { get; set; } = 6;

    public bool IsActive { get; set; } = true;
}