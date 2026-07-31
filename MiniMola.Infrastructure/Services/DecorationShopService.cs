using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Shop;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class DecorationShopService(
    ApplicationDbContext dbContext)
    : IDecorationShopService
{
    public async Task<DecorationShopDto?> GetShopAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            identityUserId);

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
                x.Level
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (aquarium is null)
        {
            return null;
        }

        var placedDecorationCount =
            await dbContext.UserDecorations.CountAsync(
                x => x.AquariumId == aquarium.Id,
                cancellationToken);

        var ownedCounts = await dbContext.UserDecorations
            .AsNoTracking()
            .Where(x => x.UserProfileId == profile.Id)
            .GroupBy(x => x.DecorationItemId)
            .Select(group => new
            {
                DecorationItemId = group.Key,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                x => x.DecorationItemId,
                x => x.Count,
                cancellationToken);

        var decorations = await dbContext.DecorationItems
            .AsNoTracking()
            .Where(x => x.IsActive && x.Price > 0)
            .OrderBy(x => x.RequiredAquariumLevel)
            .ThenBy(x => x.Price)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.AssetKey,
                x.Category,
                x.Price,
                x.RequiredAquariumLevel,
                x.DisplayScale
            })
            .ToListAsync(cancellationToken);

        var items = decorations
            .Select(x =>
            {
                string? lockedReason = null;

                if (aquarium.Level
                    < x.RequiredAquariumLevel)
                {
                    lockedReason =
                        $"Akvaryum seviyesi "
                        + $"{x.RequiredAquariumLevel} olmalı.";
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

                return new DecorationShopItemDto(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.AssetKey,
                    x.Category.ToString(),
                    x.Price,
                    x.RequiredAquariumLevel,
                    x.DisplayScale,
                    ownedCount,
                    lockedReason is null,
                    lockedReason);
            })
            .ToList();

        return new DecorationShopDto(
            profile.PointBalance,
            aquarium.Level,
            placedDecorationCount,
            items);
    }

    public async Task<PurchaseDecorationResultDto>
        PurchaseAsync(
            string identityUserId,
            int decorationItemId,
            CancellationToken cancellationToken = default)
    {
        if (decorationItemId <= 0)
        {
            return Failed(
                "Geçersiz dekorasyon seçimi.");
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
            return Failed(
                "Kullanıcı profili bulunamadı.");
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

        var decoration =
            await dbContext.DecorationItems
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == decorationItemId
                         && x.IsActive,
                    cancellationToken);

        if (decoration is null
            || decoration.Price <= 0)
        {
            return Failed(
                "Bu dekorasyon şu anda satışta değil.",
                profile.PointBalance);
        }

        if (aquarium.Level
            < decoration.RequiredAquariumLevel)
        {
            return Failed(
                $"Bu dekorasyon için akvaryum seviyesi "
                + $"{decoration.RequiredAquariumLevel} olmalı.",
                profile.PointBalance);
        }

        if (profile.PointBalance < decoration.Price)
        {
            return Failed(
                "Bu dekorasyonu almak için yeterli Damlan yok.",
                profile.PointBalance);
        }

        var placedDecorationCount =
            await dbContext.UserDecorations.CountAsync(
                x => x.AquariumId == aquarium.Id,
                cancellationToken);

        var defaultPosition =
            GetDefaultPosition(placedDecorationCount);

        profile.PointBalance -= decoration.Price;
        profile.UpdatedAtUtc = DateTime.UtcNow;

        var userDecoration = new UserDecoration
        {
            UserProfileId = profile.Id,
            DecorationItemId = decoration.Id,
            AquariumId = aquarium.Id,
            PositionX = defaultPosition.X,
            PositionY = defaultPosition.Y,
            ZIndex = 10 + placedDecorationCount,
            Rotation = defaultPosition.Rotation,
            Scale = decoration.DisplayScale,
            AcquiredAtUtc = DateTime.UtcNow
        };

        var pointTransaction = new PointTransaction
        {
            UserProfileId = profile.Id,
            TransactionType =
                PointTransactionType.DecorationPurchase,
            Amount = -decoration.Price,
            BalanceAfter = profile.PointBalance,
            ReferenceId =
                $"decoration-purchase-{Guid.NewGuid():N}",
            Description =
                $"{decoration.Name} satın alındı"
        };

        dbContext.UserDecorations.Add(userDecoration);
        dbContext.PointTransactions.Add(pointTransaction);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new PurchaseDecorationResultDto(
            true,
            $"{decoration.Name} akvaryumuna yerleştirildi.",
            profile.PointBalance,
            userDecoration.Id,
            true);
    }

    private static (
        float X,
        float Y,
        float Rotation)
        GetDefaultPosition(int placedDecorationCount)
    {
        float[] horizontalPositions =
        [
            0.12f,
            0.27f,
            0.73f,
            0.88f,
            0.42f,
            0.58f
        ];

        var positionIndex =
            placedDecorationCount
            % horizontalPositions.Length;

        var row =
            (placedDecorationCount
             / horizontalPositions.Length)
            % 2;

        var positionY =
            row == 0
                ? 0.79f
                : 0.70f;

        var rotation =
            positionIndex % 2 == 0
                ? -0.04f
                : 0.04f;

        return (
            horizontalPositions[positionIndex],
            positionY,
            rotation);
    }

    private static PurchaseDecorationResultDto Failed(
        string message,
        int pointBalance = 0)
    {
        return new PurchaseDecorationResultDto(
            false,
            message,
            pointBalance,
            null,
            false);
    }
}