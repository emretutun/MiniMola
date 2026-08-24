using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class UserFishConfiguration
    : IEntityTypeConfiguration<UserFish>
{
    public void Configure(EntityTypeBuilder<UserFish> builder)
    {
        builder.ToTable("UserFish");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nickname)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.ColorVariantKey)
            .HasMaxLength(100);

        builder.Property(x => x.TotalFeedings)
            .HasDefaultValue(0);

        builder.HasIndex(x => x.UserProfileId);

        builder.HasIndex(x => x.FishSpeciesId);

        builder.HasIndex(x => x.AquariumId);

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FishSpecies)
            .WithMany()
            .HasForeignKey(x => x.FishSpeciesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Aquarium)
            .WithMany()
            .HasForeignKey(x => x.AquariumId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}