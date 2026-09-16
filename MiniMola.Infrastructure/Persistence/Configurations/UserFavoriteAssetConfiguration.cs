using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class UserFavoriteAssetConfiguration
    : IEntityTypeConfiguration<UserFavoriteAsset>
{
    public void Configure(
        EntityTypeBuilder<UserFavoriteAsset> builder)
    {
        builder.ToTable(
            "UserFavoriteAssets",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_UserFavoriteAssets_SortOrder",
                    "[SortOrder] >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SortOrder)
            .HasDefaultValue(0);

        builder.HasIndex(
                x => new
                {
                    x.UserProfileId,
                    x.MarketAssetId
                })
            .IsUnique();

        builder.HasIndex(
            x => new
            {
                x.UserProfileId,
                x.SortOrder
            });

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MarketAsset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}