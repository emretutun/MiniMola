using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.WorkSchedules;

public sealed record WorkCountdownDto(
    bool HasSchedule,
    bool IsWorkingDay,
    bool IsBeforeWork,
    bool IsWorkingNow,
    bool IsWorkCompleted,
    string StatusMessage,
    DateTimeOffset CurrentTime,
    DateTimeOffset? WorkStartTime,
    DateTimeOffset? WorkEndTime,
    long RemainingSeconds,
    int ProgressPercentage);