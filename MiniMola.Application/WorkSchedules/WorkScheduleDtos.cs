using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMola.Application.WorkSchedules;

public sealed record WorkScheduleDayDto(
    DayOfWeek DayOfWeek,
    string DayName,
    bool IsWorkingDay,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record WorkScheduleDto(
    IReadOnlyList<WorkScheduleDayDto> Days);

public sealed record UpdateWorkScheduleDayRequest(
    DayOfWeek DayOfWeek,
    bool IsWorkingDay,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record UpdateWorkScheduleRequest(
    IReadOnlyList<UpdateWorkScheduleDayRequest> Days);

public sealed record UpdateWorkScheduleResultDto(
    bool Success,
    string Message,
    WorkScheduleDto? Schedule);