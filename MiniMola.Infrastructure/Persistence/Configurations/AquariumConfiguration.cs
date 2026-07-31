using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class AquariumConfiguration
    : IEntityTypeConfiguration<Aquarium>
{
    public void Configure(EntityTypeBuilder<Aquarium> builder)
    {
        builder.ToTable(
            "Aquariums",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Aquariums_Level_Positive",
                    "[Level] >= 1");

                table.HasCheckConstraint(
                    "CK_Aquariums_Capacity_Positive",
                    "[Capacity] >= 1");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(x => x.ThemeKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Level)
            .HasDefaultValue(1);

        builder.Property(x => x.Capacity)
            .HasDefaultValue(5);

        builder.HasIndex(x => x.UserProfileId)
            .IsUnique();

        builder.HasOne(x => x.UserProfile)
            .WithOne()
            .HasForeignKey<Aquarium>(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}