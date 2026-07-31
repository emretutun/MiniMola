using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class UserDecorationConfiguration
    : IEntityTypeConfiguration<UserDecoration>
{
    public void Configure(EntityTypeBuilder<UserDecoration> builder)
    {
        builder.ToTable(
            "UserDecorations",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_UserDecorations_PositionX_Range",
                    "[PositionX] >= 0 AND [PositionX] <= 1");

                table.HasCheckConstraint(
                    "CK_UserDecorations_PositionY_Range",
                    "[PositionY] >= 0 AND [PositionY] <= 1");

                table.HasCheckConstraint(
                    "CK_UserDecorations_Scale_Positive",
                    "[Scale] > 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Scale)
            .HasDefaultValue(1.0f);

        builder.HasIndex(x => x.UserProfileId);

        builder.HasIndex(x => x.DecorationItemId);

        builder.HasIndex(x => x.AquariumId);

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DecorationItem)
            .WithMany()
            .HasForeignKey(x => x.DecorationItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Aquarium)
            .WithMany()
            .HasForeignKey(x => x.AquariumId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}