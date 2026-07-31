using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Aquariums;
using MiniMola.Infrastructure.Persistence;
using System.Data;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Services;

public sealed class AquariumService(
    ApplicationDbContext dbContext)
    : IAquariumService
{
    public async Task<AquariumDetailsDto?> GetByIdentityUserIdAsync(
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
                x.FishSpecies.AssetKey,
                x.Nickname,
                x.ColorVariantKey,
                x.FishSpecies.Rarity,
                x.FishSpecies.BaseSpeed,
                x.FishSpecies.DisplayScale
            })
            .ToListAsync(cancellationToken);

        var fish = fishData
            .Select(x => new AquariumFishDto(
                x.Id,
                x.SpeciesName,
                x.AssetKey,
                x.Nickname,
                x.ColorVariantKey,
                x.Rarity.ToString(),
                x.BaseSpeed,
                x.DisplayScale))
            .ToList();

        var decorationData = await dbContext.UserDecorations
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
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        var profile = await dbContext.UserProfiles
            .SingleOrDefaultAsync(
                x => x.IdentityUserId == identityUserId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Kullanıcı profili bulunamadı.");

        var aquarium = await dbContext.Aquariums
            .SingleOrDefaultAsync(
                x => x.UserProfileId == profile.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
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

        dbContext.PointTransactions.Add(pointTransaction);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return BuildUpgradeDto(
            aquarium.Level,
            aquarium.Capacity,
            profile.PointBalance);
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