using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class SpotifyConnectionConfiguration
    : IEntityTypeConfiguration<SpotifyConnection>
{
    public void Configure(
        EntityTypeBuilder<SpotifyConnection> builder)
    {
        builder.ToTable("SpotifyConnections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SpotifyAccountId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SpotifyUserId)
            .HasMaxLength(200);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ProfileImageUrl)
            .HasMaxLength(1000);

        builder.Property(x => x.ProtectedAccessToken)
            .IsRequired();

        builder.Property(x => x.ProtectedRefreshToken)
            .IsRequired();

        builder.Property(x => x.GrantedScopes)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasIndex(x => x.UserProfileId)
            .IsUnique();

        builder.HasIndex(x => x.SpotifyAccountId)
            .IsUnique();

        builder.HasOne(x => x.UserProfile)
            .WithOne()
            .HasForeignKey<SpotifyConnection>(
                x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}