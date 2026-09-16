using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.WorkSchedules;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Services;

public sealed class WorkScheduleService(
    ApplicationDbContext dbContext,
    ILogger<WorkScheduleService> logger)
    : IWorkScheduleService
{
    private static readonly DayOfWeek[] DayOrder =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday
    ];
    private static readonly TimeZoneInfo TurkeyTimeZone =
    FindTurkeyTimeZone();


    public async Task<WorkScheduleDto?> GetAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        var userProfileId =
            await dbContext.UserProfiles
                .AsNoTracking()
                .Where(x =>
                    x.IdentityUserId == identityUserId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!userProfileId.HasValue)
        {
            return null;
        }

        var savedDays =
            await dbContext.UserWorkScheduleDays
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId
                    == userProfileId.Value)
                .ToListAsync(cancellationToken);

        var savedDayLookup =
            savedDays.ToDictionary(
                x => x.DayOfWeek);

        var days = DayOrder
            .Select(dayOfWeek =>
            {
                if (savedDayLookup.TryGetValue(
                        dayOfWeek,
                        out var savedDay))
                {
                    return new WorkScheduleDayDto(
                        savedDay.DayOfWeek,
                        GetTurkishDayName(
                            savedDay.DayOfWeek),
                        savedDay.IsWorkingDay,
                        savedDay.StartTime,
                        savedDay.EndTime);
                }

                var isDefaultWorkingDay =
                    dayOfWeek is
                        DayOfWeek.Monday
                        or DayOfWeek.Tuesday
                        or DayOfWeek.Wednesday
                        or DayOfWeek.Thursday
                        or DayOfWeek.Friday;

                return new WorkScheduleDayDto(
                    dayOfWeek,
                    GetTurkishDayName(dayOfWeek),
                    isDefaultWorkingDay,
                    new TimeOnly(9, 0),
                    new TimeOnly(18, 0));
            })
            .ToList();

        return new WorkScheduleDto(days);
    }

    public async Task<UpdateWorkScheduleResultDto>
    UpdateAsync(
        string identityUserId,
        UpdateWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        ArgumentNullException.ThrowIfNull(request);

        if (request.Days is null
            || request.Days.Count != 7
            || request.Days
                .Select(x => x.DayOfWeek)
                .Distinct()
                .Count() != 7)
        {
            return new UpdateWorkScheduleResultDto(
                false,
                "Haftanın yedi günü için geçerli "
                + "çalışma bilgisi gönderilmelidir.",
                null);
        }

        if (!request.Days.Any(x => x.IsWorkingDay))
        {
            return new UpdateWorkScheduleResultDto(
                false,
                "En az bir çalışma günü seçmelisin.",
                null);
        }

        var userProfileId =
            await dbContext.UserProfiles
                .AsNoTracking()
                .Where(x =>
                    x.IdentityUserId == identityUserId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!userProfileId.HasValue)
        {
            return new UpdateWorkScheduleResultDto(
                false,
                "Kullanıcı profili bulunamadı.",
                null);
        }

        var existingDays =
            await dbContext.UserWorkScheduleDays
                .Where(x =>
                    x.UserProfileId
                    == userProfileId.Value)
                .ToListAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        foreach (var requestedDay in request.Days)
        {
            var existingDay =
                existingDays.SingleOrDefault(x =>
                    x.DayOfWeek
                    == requestedDay.DayOfWeek);

            if (existingDay is null)
            {
                dbContext.UserWorkScheduleDays.Add(
                    new UserWorkScheduleDay
                    {
                        UserProfileId =
                            userProfileId.Value,

                        DayOfWeek =
                            requestedDay.DayOfWeek,

                        IsWorkingDay =
                            requestedDay.IsWorkingDay,

                        StartTime =
                            requestedDay.StartTime,

                        EndTime =
                            requestedDay.EndTime
                    });

                continue;
            }

            existingDay.IsWorkingDay =
                requestedDay.IsWorkingDay;

            existingDay.StartTime =
                requestedDay.StartTime;

            existingDay.EndTime =
                requestedDay.EndTime;

            existingDay.UpdatedAtUtc = nowUtc;
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Çalışma takvimi güncellendi. "
            + "UserProfileId: {UserProfileId}, "
            + "WorkingDayCount: {WorkingDayCount}",
            userProfileId.Value,
            request.Days.Count(x =>
                x.IsWorkingDay));

        var schedule = await GetAsync(
            identityUserId,
            cancellationToken);

        return new UpdateWorkScheduleResultDto(
            true,
            "Çalışma saatlerin kaydedildi.",
            schedule);
    }

    public async Task<WorkCountdownDto?>
        GetCountdownAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        var userProfileId =
            await dbContext.UserProfiles
                .AsNoTracking()
                .Where(x =>
                    x.IdentityUserId == identityUserId)
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (!userProfileId.HasValue)
        {
            return null;
        }

        var currentTime =
            TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                TurkeyTimeZone);

        var scheduleDays =
            await dbContext.UserWorkScheduleDays
                .AsNoTracking()
                .Where(x =>
                    x.UserProfileId
                    == userProfileId.Value)
                .ToListAsync(cancellationToken);

        if (scheduleDays.Count == 0)
        {
            return new WorkCountdownDto(
                false,
                false,
                false,
                false,
                false,
                "Geri sayımı başlatmak için "
                + "çalışma saatlerini kaydetmelisin.",
                currentTime,
                null,
                null,
                0,
                0);
        }

        var currentDate =
            DateOnly.FromDateTime(
                currentTime.DateTime);

        var scheduleLookup =
            scheduleDays.ToDictionary(
                x => x.DayOfWeek);

        var previousDate =
            currentDate.AddDays(-1);

        if (scheduleLookup.TryGetValue(
                previousDate.DayOfWeek,
                out var previousDay)
            && previousDay.IsWorkingDay
            && previousDay.EndTime
                < previousDay.StartTime)
        {
            var previousShiftStart =
                CreateLocalDateTime(
                    previousDate,
                    previousDay.StartTime);

            var previousShiftEnd =
                CreateLocalDateTime(
                    currentDate,
                    previousDay.EndTime);

            if (currentTime >= previousShiftStart
                && currentTime < previousShiftEnd)
            {
                return BuildActiveCountdown(
                    currentTime,
                    previousShiftStart,
                    previousShiftEnd);
            }
        }

        if (!scheduleLookup.TryGetValue(
                currentDate.DayOfWeek,
                out var currentDay)
            || !currentDay.IsWorkingDay)
        {
            return new WorkCountdownDto(
                true,
                false,
                false,
                false,
                false,
                "Bugün çalışma günün değil.",
                currentTime,
                null,
                null,
                0,
                0);
        }

        var workStartTime =
            CreateLocalDateTime(
                currentDate,
                currentDay.StartTime);

        var workEndDate =
            currentDay.EndTime
                < currentDay.StartTime
                    ? currentDate.AddDays(1)
                    : currentDate;

        var workEndTime =
            CreateLocalDateTime(
                workEndDate,
                currentDay.EndTime);

        if (currentTime < workStartTime)
        {
            return new WorkCountdownDto(
                true,
                true,
                true,
                false,
                false,
                $"Mesain {currentDay.StartTime:HH\\:mm}"
                + " saatinde başlayacak.",
                currentTime,
                workStartTime,
                workEndTime,
                0,
                0);
        }

        if (currentTime >= workEndTime)
        {
            return new WorkCountdownDto(
                true,
                true,
                false,
                false,
                true,
                "Bugünkü mesain tamamlandı.",
                currentTime,
                workStartTime,
                workEndTime,
                0,
                100);
        }

        return BuildActiveCountdown(
            currentTime,
            workStartTime,
            workEndTime);
    }

    private static WorkCountdownDto BuildActiveCountdown(
    DateTimeOffset currentTime,
    DateTimeOffset workStartTime,
    DateTimeOffset workEndTime)
    {
        var remainingSeconds = Math.Max(
            0,
            (long)Math.Ceiling(
                (workEndTime - currentTime)
                    .TotalSeconds));

        var totalSeconds =
            (workEndTime - workStartTime)
                .TotalSeconds;

        var elapsedSeconds = Math.Clamp(
            (currentTime - workStartTime)
                .TotalSeconds,
            0,
            totalSeconds);

        var progressPercentage =
            totalSeconds <= 0
                ? 0
                : Math.Clamp(
                    (int)Math.Floor(
                        elapsedSeconds
                        / totalSeconds
                        * 100),
                    0,
                    100);

        return new WorkCountdownDto(
            true,
            true,
            false,
            true,
            false,
            "Mesain devam ediyor.",
            currentTime,
            workStartTime,
            workEndTime,
            remainingSeconds,
            progressPercentage);
    }

    private static DateTimeOffset CreateLocalDateTime(
        DateOnly date,
        TimeOnly time)
    {
        var localDateTime =
            date.ToDateTime(
                time,
                DateTimeKind.Unspecified);

        var utcOffset =
            TurkeyTimeZone.GetUtcOffset(
                localDateTime);

        return new DateTimeOffset(
            localDateTime,
            utcOffset);
    }

    private static TimeZoneInfo FindTurkeyTimeZone()
    {
        string[] timeZoneIds =
        [
            "Europe/Istanbul",
        "Turkey Standard Time"
        ];

        foreach (var timeZoneId in timeZoneIds)
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Diğer kimlik denenecek.
            }
            catch (InvalidTimeZoneException)
            {
                // Diğer kimlik denenecek.
            }
        }

        return TimeZoneInfo.Utc;
    }

    private static string GetTurkishDayName(
        DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Monday => "Pazartesi",
            DayOfWeek.Tuesday => "Salı",
            DayOfWeek.Wednesday => "Çarşamba",
            DayOfWeek.Thursday => "Perşembe",
            DayOfWeek.Friday => "Cuma",
            DayOfWeek.Saturday => "Cumartesi",
            DayOfWeek.Sunday => "Pazar",
            _ => "Bilinmeyen gün"
        };
    }
}