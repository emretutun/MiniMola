using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class PointTransactionConfiguration
    : IEntityTypeConfiguration<PointTransaction>
{
    public void Configure(EntityTypeBuilder<PointTransaction> builder)
    {
        builder.ToTable(
            "PointTransactions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_PointTransactions_Amount_NotZero",
                    "[Amount] <> 0");

                table.HasCheckConstraint(
                    "CK_PointTransactions_BalanceAfter_NonNegative",
                    "[BalanceAfter] >= 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.ReferenceId)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(x => new
        {
            x.UserProfileId,
            x.ReferenceId
        })
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.UserProfileId,
            x.CreatedAtUtc
        });

        builder.HasOne(x => x.UserProfile)
            .WithMany()
            .HasForeignKey(x => x.UserProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}