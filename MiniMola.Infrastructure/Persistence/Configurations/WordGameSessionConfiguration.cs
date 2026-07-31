using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class WordGameSessionConfiguration
    : IEntityTypeConfiguration<WordGameSession>
{
    public void Configure(
        EntityTypeBuilder<WordGameSession> builder)
    {
        builder.ToTable(
            "WordGameSessions",
            table => table.HasCheckConstraint(
                "CK_WordGameSessions_AttemptCount_NonNegative",
                "[AttemptCount] >= 0"));

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.AttemptCount)
            .HasDefaultValue(0);

        builder.Property(x => x.RewardGranted)
            .HasDefaultValue(false);

        builder.HasIndex(x => new
        {
            x.UserProfileId,
            x.DailyWordPuzzleId
        })
            .IsUnique();

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DailyWordPuzzle)
            .WithMany()
            .HasForeignKey(x => x.DailyWordPuzzleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}