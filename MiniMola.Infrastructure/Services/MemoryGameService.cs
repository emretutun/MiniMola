using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.MemoryGames;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class MemoryGameService(
    ApplicationDbContext dbContext)
    : IMemoryGameService
{
    private const int PairCount = 6;
    private const int RewardPoints = 60;

    private static readonly TimeZoneInfo TurkeyTimeZone =
        FindTurkeyTimeZone();

    public async Task<MemoryGameStatusDto?> GetStatusAsync(
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
            ? "Bugünkü Hafıza Kartları ödülünü zaten kazandın."
            : $"{PairCount} kart çiftini bul ve "
              + $"{RewardPoints} Damla kazan.";

        return new MemoryGameStatusDto(
            today,
            PairCount,
            RewardPoints,
            profile.PointBalance,
            rewardClaimed,
            message);
    }

    public async Task<MemoryGameResultDto> CompleteAsync(
        string identityUserId,
        int matchedPairs,
        int moveCount,
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
            return Failed(
                "Kullanıcı profili bulunamadı.",
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
            return Failed(
                "Bugünkü ödülünü zaten kazandın.",
                profile.PointBalance,
                true);
        }

        if (matchedPairs != PairCount)
        {
            return Failed(
                "Tüm kart çiftlerini bulmalısın.",
                profile.PointBalance,
                false);
        }

        if (moveCount < PairCount || moveCount > 1000)
        {
            return Failed(
                "Geçersiz hamle sayısı.",
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
                    "Günlük Hafıza Kartları oyunu ödülü"
            });

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new MemoryGameResultDto(
            true,
            $"Tebrikler! {moveCount} hamlede tamamladın "
            + $"ve {RewardPoints} Damla kazandın.",
            RewardPoints,
            profile.PointBalance,
            true);
    }

    private static MemoryGameResultDto Failed(
        string message,
        int pointBalance,
        bool rewardClaimed)
    {
        return new MemoryGameResultDto(
            false,
            message,
            0,
            pointBalance,
            rewardClaimed);
    }

    private static string CreateReferenceId(
        DateOnly gameDate)
    {
        return $"memory-game-{gameDate:yyyy-MM-dd}";
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