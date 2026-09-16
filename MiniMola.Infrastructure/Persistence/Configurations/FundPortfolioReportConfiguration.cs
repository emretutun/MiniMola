using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class FundPortfolioReportConfiguration
    : IEntityTypeConfiguration<FundPortfolioReport>
{
    public void Configure(
        EntityTypeBuilder<FundPortfolioReport> builder)
    {
        builder.ToTable(
            "FundPortfolioReports",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_FundPortfolioReports_ParsedWeightPercent",
                    "[ParsedWeightPercent] >= -100 AND " +
                    "[ParsedWeightPercent] <= 300");

                table.HasCheckConstraint(
                    "CK_FundPortfolioReports_MatchedWeightPercent",
                    "[MatchedWeightPercent] >= -100 AND " +
                    "[MatchedWeightPercent] <= 300");
            });

        builder.HasKey(report => report.Id);

        builder.Property(report => report.DocumentObjectId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(report => report.FileName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(report => report.DocumentUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(report => report.NotificationUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(report => report.ParserVersion)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(report => report.ParsedWeightPercent)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.Property(report => report.MatchedWeightPercent)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.HasIndex(report => report.DocumentObjectId)
            .IsUnique();

        builder.HasIndex(
                report => new
                {
                    report.FundMarketAssetId,
                    report.ReportDate
                })
            .IsUnique();

        builder.HasOne(report => report.FundMarketAsset)
            .WithMany()
            .HasForeignKey(report => report.FundMarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
