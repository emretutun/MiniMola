using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Shop;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class FishShopService(
    ApplicationDbContext dbContext)
    : IFishShopService
{
    public async Task<FishShopDto?> GetShopAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.IdentityUserId == identityUserId)
            .Select(x => new
            {
                x.Id,
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
                x.Level,
                x.Capacity
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (aquarium is null)
        {
            return null;
        }

        var placedFishCount = await dbContext.UserFish
            .CountAsync(
                x => x.AquariumId == aquarium.Id,
                cancellationToken);

        var ownedCounts = await dbContext.UserFish
            .AsNoTracking()
            .Where(x => x.UserProfileId == profile.Id)
            .GroupBy(x => x.FishSpeciesId)
            .Select(group => new
            {
                FishSpeciesId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                x => x.FishSpeciesId,
                x => x.Count,
                cancellationToken);

        var species = await dbContext.FishSpecies
            .AsNoTracking()
            .Where(x => x.IsActive && x.Price > 0)
            .OrderBy(x => x.Price)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.AssetKey,
                x.Price,
                x.Rarity,
                x.RequiredAquariumLevel
            })
            .ToListAsync(cancellationToken);

        var items = species
            .Select(x =>
            {
                string? lockedReason = null;

                if (aquarium.Level < x.RequiredAquariumLevel)
                {
                    lockedReason =
                        $"Akvaryum seviyesi {x.RequiredAquariumLevel} olmalı.";
                }
                else if (profile.PointBalance < x.Price)
                {
                    var missingPoints =
                        x.Price - profile.PointBalance;

                    lockedReason =
                        $"{missingPoints} Damla daha gerekiyor.";
                }

                ownedCounts.TryGetValue(
                    x.Id,
                    out var ownedCount);

                return new FishShopItemDto(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.AssetKey,
                    x.Price,
                    x.Rarity.ToString(),
                    x.RequiredAquariumLevel,
                    ownedCount,
                    lockedReason is null,
                    lockedReason);
            })
            .ToList();

        return new FishShopDto(
            profile.PointBalance,
            aquarium.Level,
            aquarium.Capacity,
            placedFishCount,
            items);
    }

    public async Task<PurchaseFishResultDto> PurchaseFishAsync(
        string identityUserId,
        int fishSpeciesId,
        CancellationToken cancellationToken = default)
    {
        if (fishSpeciesId <= 0)
        {
            return Failed("Geçersiz balık seçimi.");
        }

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
            return Failed("Kullanıcı profili bulunamadı.");
        }

        var aquarium = await dbContext.Aquariums
            .SingleOrDefaultAsync(
                x => x.UserProfileId == profile.Id,
                cancellationToken);

        if (aquarium is null)
        {
            return Failed(
                "Kullanıcının akvaryumu bulunamadı.",
                profile.PointBalance);
        }

        var species = await dbContext.FishSpecies
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == fishSpeciesId
                     && x.IsActive,
                cancellationToken);

        if (species is null || species.Price <= 0)
        {
            return Failed(
                "Bu balık şu anda satışta değil.",
                profile.PointBalance);
        }

        if (aquarium.Level < species.RequiredAquariumLevel)
        {
            return Failed(
                $"Bu balık için akvaryum seviyesi "
                + $"{species.RequiredAquariumLevel} olmalı.",
                profile.PointBalance);
        }

        if (profile.PointBalance < species.Price)
        {
            return Failed(
                "Bu balığı almak için yeterli Damlan yok.",
                profile.PointBalance);
        }

        var placedFishCount = await dbContext.UserFish
            .CountAsync(
                x => x.AquariumId == aquarium.Id,
                cancellationToken);

        var willBePlaced =
            placedFishCount < aquarium.Capacity;

        profile.PointBalance -= species.Price;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        var nickname = species.Name.Length <= 40
            ? species.Name
            : species.Name[..40];

        var userFish = new UserFish
        {
            UserProfileId = profile.Id,
            FishSpeciesId = species.Id,
            AquariumId = willBePlaced
                ? aquarium.Id
                : null,
            Nickname = nickname
        };

        var pointTransaction = new PointTransaction
        {
            UserProfileId = profile.Id,
            TransactionType =
                PointTransactionType.FishPurchase,
            Amount = -species.Price,
            BalanceAfter = profile.PointBalance,
            ReferenceId =
                $"fish-purchase-{Guid.NewGuid():N}",
            Description =
                $"{species.Name} satın alındı"
        };

        dbContext.UserFish.Add(userFish);
        dbContext.PointTransactions.Add(pointTransaction);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var message = willBePlaced
            ? $"{species.Name} akvaryumuna eklendi."
            : $"{species.Name} satın alındı ve envanterine eklendi.";

        return new PurchaseFishResultDto(
            true,
            message,
            profile.PointBalance,
            userFish.Id,
            willBePlaced);
    }

    private static PurchaseFishResultDto Failed(
        string message,
        int pointBalance = 0)
    {
        return new PurchaseFishResultDto(
            false,
            message,
            pointBalance,
            null,
            false);
    }
}