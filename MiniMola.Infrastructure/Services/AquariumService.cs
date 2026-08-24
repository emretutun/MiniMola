using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Aquariums;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;
using MiniMola.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace MiniMola.Infrastructure.Services;

public sealed class AquariumService(
    ApplicationDbContext dbContext,
    ILogger<AquariumService> logger)
    : IAquariumService
{
    public async Task<AquariumDetailsDto?>
        GetByIdentityUserIdAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.IdentityUserId == identityUserId)
            .Select(x => new
            {
                x.Id,
                x.DisplayName,
                x.PointBalance
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var aquarium = await dbContext.Aquariums
            .AsNoTracking()
            .Where(x => x.UserProfileId == profile.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ThemeKey,
                x.Level,
                x.Capacity
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (aquarium is null)
        {
            return null;
        }

        var fishData = await dbContext.UserFish
            .AsNoTracking()
            .Where(x => x.AquariumId == aquarium.Id)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                SpeciesName = x.FishSpecies.Name,
                SpeciesDescription =
                    x.FishSpecies.Description,
                x.FishSpecies.AssetKey,
                x.Nickname,
                x.AcquiredAtUtc,
                x.LastFedAtUtc,
                x.TotalFeedings,
                x.ColorVariantKey,
                x.FishSpecies.Rarity,
                x.FishSpecies.BaseSpeed,
                x.FishSpecies.DisplayScale
            })
            .ToListAsync(cancellationToken);

        var nowUtc = DateTime.UtcNow;

        var fish = fishData
            .Select(x =>
            {
                var happinessPercent = 35;
                var careStatus = "Acıkmış";
                var canFeed = true;
                DateTime? nextFeedAtUtc = null;

                if (x.LastFedAtUtc.HasValue)
                {
                    var lastFedAtUtc =
                        x.LastFedAtUtc.Value;

                    var hoursSinceFed = Math.Max(
                        0,
                        (nowUtc - lastFedAtUtc).TotalHours);

                    happinessPercent = Math.Clamp(
                        100
                        - ((int)Math.Floor(
                            hoursSinceFed / 6) * 10),
                        20,
                        100);

                    careStatus = happinessPercent switch
                    {
                        >= 80 => "Mutlu",
                        >= 50 => "İyi",
                        >= 30 => "Acıkmış",
                        _ => "Çok aç"
                    };

                    var feedAvailableAtUtc =
                        lastFedAtUtc.AddHours(4);

                    canFeed =
                        nowUtc >= feedAvailableAtUtc;

                    nextFeedAtUtc =
                        canFeed
                            ? null
                            : feedAvailableAtUtc;
                }

                return new AquariumFishDto(
                    x.Id,
                    x.SpeciesName,
                    x.SpeciesDescription,
                    x.AssetKey,
                    x.Nickname,
                    x.AcquiredAtUtc,
                    x.ColorVariantKey,
                    x.Rarity.ToString(),
                    x.BaseSpeed,
                    x.DisplayScale,
                    x.LastFedAtUtc,
                    x.TotalFeedings,
                    happinessPercent,
                    careStatus,
                    canFeed,
                    nextFeedAtUtc);
            })
            .ToList();

        var decorationData =
            await dbContext.UserDecorations
                .AsNoTracking()
                .Where(x => x.AquariumId == aquarium.Id)
                .OrderBy(x => x.ZIndex)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    Name = x.DecorationItem.Name,
                    x.DecorationItem.AssetKey,
                    x.DecorationItem.Category,
                    x.PositionX,
                    x.PositionY,
                    x.ZIndex,
                    x.Rotation,
                    x.Scale
                })
                .ToListAsync(cancellationToken);

        var decorations = decorationData
            .Select(x => new AquariumDecorationDto(
                x.Id,
                x.Name,
                x.AssetKey,
                x.Category.ToString(),
                x.PositionX,
                x.PositionY,
                x.ZIndex,
                x.Rotation,
                x.Scale))
            .ToList();

        return new AquariumDetailsDto(
            aquarium.Id,
            aquarium.Name,
            aquarium.ThemeKey,
            aquarium.Level,
            aquarium.Capacity,
            profile.DisplayName,
            profile.PointBalance,
            fish,
            decorations);
    }

    public async Task<AquariumUpgradeDto?>
        GetUpgradeStatusAsync(
            string identityUserId,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        var data = await dbContext.Aquariums
            .AsNoTracking()
            .Where(
                x => x.UserProfile.IdentityUserId
                     == identityUserId)
            .Select(
                x => new
                {
                    x.Level,
                    x.Capacity,
                    x.UserProfile.PointBalance
                })
            .SingleOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return null;
        }

        return BuildUpgradeDto(
            data.Level,
            data.Capacity,
            data.PointBalance);
    }

    public async Task<AquariumUpgradeDto> UpgradeAsync(
        string identityUserId,
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
        cancellationToken)
    ?? throw new NotFoundException(
        "Kullanıcı profili bulunamadı.");

        var aquarium = await dbContext.Aquariums
            .SingleOrDefaultAsync(
                x => x.UserProfileId == profile.Id,
                cancellationToken)
            ?? throw new NotFoundException(
                "Akvaryum bulunamadı.");

        var currentStatus = BuildUpgradeDto(
            aquarium.Level,
            aquarium.Capacity,
            profile.PointBalance);

        if (!currentStatus.CanUpgrade
            || currentStatus.NextLevel is null
            || currentStatus.NextCapacity is null
            || currentStatus.UpgradeCost is null)
        {
            return currentStatus;
        }

        var upgradeCost =
            currentStatus.UpgradeCost.Value;

        var nextLevel =
            currentStatus.NextLevel.Value;

        var nextCapacity =
            currentStatus.NextCapacity.Value;

        profile.PointBalance -= upgradeCost;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        aquarium.Level = nextLevel;
        aquarium.Capacity = nextCapacity;
        aquarium.UpdatedAtUtc = DateTime.UtcNow;

        var pointTransaction = new PointTransaction
        {
            UserProfileId = profile.Id,
            TransactionType =
                PointTransactionType.AquariumUpgrade,
            Amount = -upgradeCost,
            BalanceAfter = profile.PointBalance,
            ReferenceId =
                $"aquarium-{aquarium.Id}-upgrade-level-{nextLevel}",
            Description =
                $"Akvaryum seviye {nextLevel} yükseltmesi"
        };

        dbContext.PointTransactions.Add(
            pointTransaction);

        await dbContext.SaveChangesAsync(
            cancellationToken);


        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Akvaryum yükseltildi. "
            + "UserProfileId: {UserProfileId}, "
            + "AquariumId: {AquariumId}, "
            + "OldLevel: {OldLevel}, "
            + "NewLevel: {NewLevel}, "
            + "UpgradeCost: {UpgradeCost}, "
            + "BalanceAfter: {BalanceAfter}, "
            + "PointTransactionId: {PointTransactionId}",
            profile.Id,
            aquarium.Id,
            currentStatus.Level,
            aquarium.Level,
            upgradeCost,
            profile.PointBalance,
            pointTransaction.Id);

        return BuildUpgradeDto(
            aquarium.Level,
            aquarium.Capacity,
            profile.PointBalance);
    }

    public async Task<FeedFishResultDto> FeedFishAsync(
    string identityUserId,
    int userFishId,
    CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        if (userFishId <= 0)
        {
            return new FeedFishResultDto(
                false,
                "Geçersiz balık seçimi.",
                userFishId,
                null,
                0,
                0,
                "Bilinmiyor",
                false,
                null);
        }

        var userFish =
            await dbContext.UserFish
                .SingleOrDefaultAsync(
                    x => x.Id == userFishId
                         && x.AquariumId != null
                         && x.UserProfile.IdentityUserId
                            == identityUserId,
                    cancellationToken);

        if (userFish is null)
        {
            return new FeedFishResultDto(
                false,
                "Akvaryumunda bu balık bulunamadı.",
                userFishId,
                null,
                0,
                0,
                "Bilinmiyor",
                false,
                null);
        }

        var nowUtc = DateTime.UtcNow;
        const int feedingCooldownHours = 4;

        if (userFish.LastFedAtUtc.HasValue)
        {
            var nextFeedAtUtc =
                userFish.LastFedAtUtc.Value.AddHours(
                    feedingCooldownHours);

            if (nowUtc < nextFeedAtUtc)
            {
                var remainingMinutes = Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (nextFeedAtUtc - nowUtc)
                        .TotalMinutes));

                var hoursSinceFed = Math.Max(
                    0,
                    (nowUtc - userFish.LastFedAtUtc.Value)
                    .TotalHours);

                var happinessPercent = Math.Clamp(
                    100
                    - ((int)Math.Floor(
                        hoursSinceFed / 6) * 10),
                    20,
                    100);

                var careStatus =
                    happinessPercent switch
                    {
                        >= 80 => "Mutlu",
                        >= 50 => "İyi",
                        >= 30 => "Acıkmış",
                        _ => "Çok aç"
                    };

                return new FeedFishResultDto(
                    false,
                    $"Balığın tok. Yaklaşık "
                    + $"{remainingMinutes} dakika sonra "
                    + "tekrar besleyebilirsin.",
                    userFish.Id,
                    userFish.LastFedAtUtc,
                    userFish.TotalFeedings,
                    happinessPercent,
                    careStatus,
                    false,
                    nextFeedAtUtc);
            }
        }

        userFish.LastFedAtUtc = nowUtc;
        userFish.TotalFeedings++;
        userFish.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var availableAtUtc =
            nowUtc.AddHours(feedingCooldownHours);

        logger.LogInformation(
            "Balık beslendi. "
            + "UserProfileId: {UserProfileId}, "
            + "UserFishId: {UserFishId}, "
            + "TotalFeedings: {TotalFeedings}, "
            + "NextFeedAtUtc: {NextFeedAtUtc}",
            userFish.UserProfileId,
            userFish.Id,
            userFish.TotalFeedings,
            availableAtUtc);

        return new FeedFishResultDto(
            true,
            $"{userFish.Nickname} yemeğini yedi ve çok mutlu oldu.",
            userFish.Id,
            userFish.LastFedAtUtc,
            userFish.TotalFeedings,
            100,
            "Mutlu",
            false,
            availableAtUtc);
    }

    public async Task<UpdateFishNicknameResultDto>
        UpdateFishNicknameAsync(
            string identityUserId,
            int userFishId,
            string nickname,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        if (userFishId <= 0)
        {
            return FailedFishNickname(
                "Geçersiz balık seçimi.",
                userFishId);
        }

        var normalizedNickname = string.Join(
            " ",
            (nickname ?? string.Empty)
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries));

        if (string.IsNullOrWhiteSpace(
                normalizedNickname))
        {
            return FailedFishNickname(
                "Balığın adı boş bırakılamaz.",
                userFishId);
        }

        if (normalizedNickname.Length > 20)
        {
            return FailedFishNickname(
                "Balığın adı en fazla 20 karakter olabilir.",
                userFishId);
        }

        var containsInvalidCharacter =
            normalizedNickname.Any(
                character =>
                    !char.IsLetterOrDigit(character)
                    && character != ' '
                    && character != '-'
                    && character != '\'');

        if (containsInvalidCharacter)
        {
            return FailedFishNickname(
                "Balık adı yalnızca harf, rakam, boşluk, tire ve kesme işareti içerebilir.",
                userFishId);
        }

        var userFish =
            await dbContext.UserFish
                .SingleOrDefaultAsync(
                    x => x.Id == userFishId
                         && x.UserProfile.IdentityUserId
                            == identityUserId,
                    cancellationToken);

        if (userFish is null)
        {
            return FailedFishNickname(
                "Balık bulunamadı.",
                userFishId);
        }

        if (string.Equals(
                userFish.Nickname,
                normalizedNickname,
                StringComparison.Ordinal))
        {
            return new UpdateFishNicknameResultDto(
                true,
                "Balığın adı zaten bu şekilde kayıtlı.",
                userFish.Id,
                userFish.Nickname);
        }

        userFish.Nickname = normalizedNickname;
        userFish.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Balık adı güncellendi. "
            + "UserProfileId: {UserProfileId}, "
            + "UserFishId: {UserFishId}",
            userFish.UserProfileId,
            userFish.Id);

        return new UpdateFishNicknameResultDto(
                    true,
            $"Balığının yeni adı {normalizedNickname} oldu.",
            userFish.Id,
            userFish.Nickname);
    }

    public async Task<UpdateDecorationPositionResultDto>
        UpdateDecorationPositionAsync(
            string identityUserId,
            int userDecorationId,
            float positionX,
            float positionY,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

        if (userDecorationId <= 0)
        {
            return FailedDecorationPosition(
                "Geçersiz dekorasyon seçimi.",
                userDecorationId);
        }

        if (!float.IsFinite(positionX)
            || !float.IsFinite(positionY))
        {
            return FailedDecorationPosition(
                "Dekorasyon konumu geçersiz.",
                userDecorationId);
        }

        if (positionX < 0
            || positionX > 1
            || positionY < 0
            || positionY > 1)
        {
            return FailedDecorationPosition(
                "Dekorasyon akvaryum sınırları dışında.",
                userDecorationId);
        }

        var userDecoration =
            await dbContext.UserDecorations
                .SingleOrDefaultAsync(
                    x => x.Id == userDecorationId
                         && x.AquariumId != null
                         && x.UserProfile.IdentityUserId
                            == identityUserId,
                    cancellationToken);

        if (userDecoration is null)
        {
            return FailedDecorationPosition(
                "Dekorasyon bulunamadı.",
                userDecorationId);
        }

        userDecoration.PositionX = Math.Clamp(
            positionX,
            0.05f,
            0.95f);

        userDecoration.PositionY = Math.Clamp(
            positionY,
            0.18f,
            0.88f);

        userDecoration.UpdatedAtUtc =
            DateTime.UtcNow;

        await dbContext.SaveChangesAsync(
            cancellationToken);

        return new UpdateDecorationPositionResultDto(
            true,
            "Dekorasyonun yeni konumu kaydedildi.",
            userDecoration.Id,
            userDecoration.PositionX,
            userDecoration.PositionY);
    }

    private static UpdateFishNicknameResultDto
        FailedFishNickname(
            string message,
            int userFishId)
    {
        return new UpdateFishNicknameResultDto(
            false,
            message,
            userFishId,
            string.Empty);
    }

    private static UpdateDecorationPositionResultDto
        FailedDecorationPosition(
            string message,
            int userDecorationId)
    {
        return new UpdateDecorationPositionResultDto(
            false,
            message,
            userDecorationId,
            0,
            0);
    }

    private static AquariumUpgradeDto BuildUpgradeDto(
        int level,
        int capacity,
        int pointBalance)
    {
        const int maximumLevel = 5;

        if (level >= maximumLevel)
        {
            return new AquariumUpgradeDto(
                level,
                capacity,
                null,
                null,
                null,
                pointBalance,
                false,
                "Akvaryumun maksimum seviyeye ulaştı.");
        }

        var upgradeCost = level switch
        {
            1 => 300,
            2 => 700,
            3 => 1300,
            4 => 2200,
            _ => throw new InvalidOperationException(
                "Geçersiz akvaryum seviyesi.")
        };

        var nextLevel = level + 1;

        var nextCapacity =
            capacity + level + 2;

        string? lockedReason = null;

        if (pointBalance < upgradeCost)
        {
            var missingPoints =
                upgradeCost - pointBalance;

            lockedReason =
                $"{missingPoints} Damla daha gerekiyor.";
        }

        return new AquariumUpgradeDto(
            level,
            capacity,
            nextLevel,
            nextCapacity,
            upgradeCost,
            pointBalance,
            lockedReason is null,
            lockedReason);
    }
}