using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class FundEstimateSnapshotConfiguration
    : IEntityTypeConfiguration<FundEstimateSnapshot>
{
    public void Configure(
        EntityTypeBuilder<FundEstimateSnapshot> builder)
    {
        builder.ToTable(
            "FundEstimateSnapshots",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_FundEstimateSnapshots_BasePrice",
                    "[BasePrice] > 0");

                table.HasCheckConstraint(
                    "CK_FundEstimateSnapshots_EstimatedPrice",
                    "[EstimatedPrice] > 0");

                table.HasCheckConstraint(
                    "CK_FundEstimateSnapshots_CoveragePercent",
                    "[CoveragePercent] >= 0 AND " +
                    "[CoveragePercent] <= 100");

                table.HasCheckConstraint(
                    "CK_FundEstimateSnapshots_ActualPrice",
                    "[ActualPrice] IS NULL OR [ActualPrice] > 0");
            });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.BasePrice)
            .HasPrecision(28, 12)
            .IsRequired();

        builder.Property(item => item.EstimatedChangePercent)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(item => item.EstimatedPrice)
            .HasPrecision(28, 12)
            .IsRequired();

        builder.Property(item => item.CoveragePercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(item => item.ConfidenceCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(item => item.ModelVersion)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(item => item.ActualPrice)
            .HasPrecision(28, 12);

        builder.Property(item => item.ActualChangePercent)
            .HasPrecision(18, 6);

        builder.Property(item => item.AbsoluteErrorPercent)
            .HasPrecision(18, 6);

        builder.HasIndex(
                item => new
                {
                    item.MarketAssetId,
                    item.TargetDate,
                    item.ModelVersion,
                    item.Kind
                })
            .IsUnique();

        builder.HasIndex(
            item => new
            {
                item.TargetDate,
                item.EvaluatedAtUtc
            });

        builder.HasOne(item => item.MarketAsset)
            .WithMany()
            .HasForeignKey(item => item.MarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
