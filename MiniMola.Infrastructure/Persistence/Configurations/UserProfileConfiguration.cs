using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration
    : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable(
            "UserProfiles",
            table => table.HasCheckConstraint(
                "CK_UserProfiles_PointBalance_NonNegative",
                "[PointBalance] >= 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdentityUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PointBalance)
            .HasDefaultValue(0);

        builder.HasIndex(x => x.IdentityUserId)
            .IsUnique();

        builder.HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(x => x.IdentityUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}