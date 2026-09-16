using System;
using System.Collections.Generic;
using System.Text;
using MiniMola.Domain.Common;

namespace MiniMola.Domain.Entities;

public sealed class UserWorkScheduleDay : BaseEntity
{
    public int UserProfileId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public bool IsWorkingDay { get; set; }

    public TimeOnly StartTime { get; set; } =
        new(9, 0);

    public TimeOnly EndTime { get; set; } =
        new(18, 0);

    public UserProfile UserProfile { get; set; } =
        null!;
}