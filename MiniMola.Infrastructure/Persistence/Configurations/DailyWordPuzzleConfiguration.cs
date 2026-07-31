using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence.Seed;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class DailyWordPuzzleConfiguration
    : IEntityTypeConfiguration<DailyWordPuzzle>
{
    public void Configure(
        EntityTypeBuilder<DailyWordPuzzle> builder)
    {
        builder.ToTable(
            "DailyWordPuzzles",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_DailyWordPuzzles_Word_Length",
                    "LEN([Word]) = 5");

                table.HasCheckConstraint(
                    "CK_DailyWordPuzzles_Reward_Positive",
                    "[RewardPoints] > 0");

                table.HasCheckConstraint(
                    "CK_DailyWordPuzzles_MaxAttempts_Range",
                    "[MaxAttempts] >= 1 AND [MaxAttempts] <= 10");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Word)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(x => x.Hint)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.RewardPoints)
            .HasDefaultValue(30);

        builder.Property(x => x.MaxAttempts)
            .HasDefaultValue(6);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.PuzzleDate)
            .IsUnique();

        builder.HasData(DailyWordPuzzleSeed.GetItems());
    }
}