using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.BubbleGames;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class BubbleGameService(
    ApplicationDbContext dbContext)
    : IBubbleGameService
{
    private const int DurationSeconds = 45;
    private const int TargetScore = 25;
    private const int RewardPoints = 40;

    private static readonly TimeZoneInfo TurkeyTimeZone =
        FindTurkeyTimeZone();

    public async Task<BubbleGameStatusDto?> GetStatusAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.IdentityUserId == identityUserId,
                cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var today = GetTurkeyToday();
        var referenceId = CreateReferenceId(today);

        var rewardClaimed =
            await dbContext.PointTransactions
                .AsNoTracking()
                .AnyAsync(
                    x => x.UserProfileId == profile.Id
                         && x.ReferenceId == referenceId,
                    cancellationToken);

        var message = rewardClaimed
            ? "Bugünkü Baloncuk Patlat ödülünü zaten kazandın."
            : $"{DurationSeconds} saniyede "
              + $"{TargetScore} baloncuk patlat ve "
              + $"{RewardPoints} Damla kazan.";

        return new BubbleGameStatusDto(
            today,
            DurationSeconds,
            TargetScore,
            RewardPoints,
            profile.PointBalance,
            rewardClaimed,
            message);
    }

    public async Task<BubbleGameResultDto> CompleteAsync(
        string identityUserId,
        int score,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(
                x => x.IdentityUserId == identityUserId,
                cancellationToken);

        if (profile is null)
        {
            return new BubbleGameResultDto(
                false,
                "Kullanıcı profili bulunamadı.",
                0,
                0,
                false);
        }

        var today = GetTurkeyToday();
        var referenceId = CreateReferenceId(today);

        var rewardClaimed =
            await dbContext.PointTransactions
                .AnyAsync(
                    x => x.UserProfileId == profile.Id
                         && x.ReferenceId == referenceId,
                    cancellationToken);

        if (rewardClaimed)
        {
            return new BubbleGameResultDto(
                false,
                "Bugünkü ödülünü zaten kazandın.",
                0,
                profile.PointBalance,
                true);
        }

        if (score < TargetScore)
        {
            var missingScore = TargetScore - score;

            return new BubbleGameResultDto(
                false,
                $"Ödül için {missingScore} baloncuk daha gerekiyor.",
                0,
                profile.PointBalance,
                false);
        }

        profile.PointBalance += RewardPoints;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.PointTransactions.Add(
            new PointTransaction
            {
                UserProfileId = profile.Id,
                TransactionType =
                    PointTransactionType.GameReward,
                Amount = RewardPoints,
                BalanceAfter = profile.PointBalance,
                ReferenceId = referenceId,
                Description =
                    "Günlük Baloncuk Patlat oyunu ödülü"
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new BubbleGameResultDto(
            true,
            $"Tebrikler! {RewardPoints} Damla kazandın.",
            RewardPoints,
            profile.PointBalance,
            true);
    }

    private static string CreateReferenceId(
        DateOnly gameDate)
    {
        return $"bubble-game-{gameDate:yyyy-MM-dd}";
    }

    private static DateOnly GetTurkeyToday()
    {
        var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            TurkeyTimeZone);

        return DateOnly.FromDateTime(turkeyTime);
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
                return TimeZoneInfo.FindSystemTimeZoneById(
                    timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Diğer platform kimliği denenecek.
            }
            catch (InvalidTimeZoneException)
            {
                // Diğer platform kimliği denenecek.
            }
        }

        return TimeZoneInfo.Utc;
    }
}