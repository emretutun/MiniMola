using Microsoft.EntityFrameworkCore;
using MiniMola.Application.Abstractions;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class UserProfileService(
    ApplicationDbContext dbContext)
    : IUserProfileService
{
    private const int WelcomeBonus = 250;
    private const int StarterFishSpeciesId = 1;

    public async Task<int> EnsureUserProfileAsync(
        string identityUserId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityUserId);

        var existingProfileId = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.IdentityUserId == identityUserId)
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (existingProfileId.HasValue)
        {
            var aquariumId = await dbContext.Aquariums
                .AsNoTracking()
                .Where(x => x.UserProfileId == existingProfileId.Value)
                .Select(x => x.Id)
                .SingleAsync(cancellationToken);

            await EnsureStarterFishAsync(
                existingProfileId.Value,
                aquariumId,
                cancellationToken);

            return existingProfileId.Value;
        }

        var safeDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? "MiniMola Kullanıcısı"
            : displayName.Trim();

        if (safeDisplayName.Length > 50)
        {
            safeDisplayName = safeDisplayName[..50];
        }

        var profile = new UserProfile
        {
            IdentityUserId = identityUserId,
            DisplayName = safeDisplayName,
            PointBalance = WelcomeBonus
        };

        var aquarium = new Aquarium
        {
            UserProfile = profile,
            Name = "Akvaryumum",
            ThemeKey = "starter-ocean",
            Level = 1,
            Capacity = 5
        };

        var starterFish = new UserFish
        {
            UserProfile = profile,
            Aquarium = aquarium,
            FishSpeciesId = StarterFishSpeciesId,
            Nickname = "Maviş"
        };

        var welcomeTransaction = new PointTransaction
        {
            UserProfile = profile,
            TransactionType = PointTransactionType.WelcomeBonus,
            Amount = WelcomeBonus,
            BalanceAfter = WelcomeBonus,
            ReferenceId = "welcome-bonus",
            Description = "MiniMola hoş geldin hediyesi"
        };

        dbContext.AddRange(
            profile,
            aquarium,
            starterFish,
            welcomeTransaction);

        await dbContext.SaveChangesAsync(cancellationToken);

        return profile.Id;
    }

    private async Task EnsureStarterFishAsync(
        int userProfileId,
        int aquariumId,
        CancellationToken cancellationToken)
    {
        var hasStarterFish = await dbContext.UserFish
            .AnyAsync(
                x => x.UserProfileId == userProfileId
                     && x.FishSpeciesId == StarterFishSpeciesId,
                cancellationToken);

        if (hasStarterFish)
        {
            return;
        }

        var starterFish = new UserFish
        {
            UserProfileId = userProfileId,
            AquariumId = aquariumId,
            FishSpeciesId = StarterFishSpeciesId,
            Nickname = "Maviş"
        };

        dbContext.UserFish.Add(starterFish);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}