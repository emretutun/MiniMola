using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class FundPortfolioHoldingConfiguration
    : IEntityTypeConfiguration<FundPortfolioHolding>
{
    public void Configure(
        EntityTypeBuilder<FundPortfolioHolding> builder)
    {
        builder.ToTable(
            "FundPortfolioHoldings",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_FundPortfolioHoldings_WeightPercent",
                    "[WeightPercent] >= -100 AND " +
                    "[WeightPercent] <= 300");
            });

        builder.HasKey(holding => holding.Id);

        builder.Property(holding => holding.SecuritySymbol)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(holding => holding.SecurityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(holding => holding.WeightPercent)
            .HasPrecision(6, 2)
            .IsRequired();

        builder.HasIndex(
                holding => new
                {
                    holding.FundPortfolioReportId,
                    holding.SecuritySymbol
                })
            .IsUnique();

        builder.HasOne(holding => holding.FundPortfolioReport)
            .WithMany(report => report.Holdings)
            .HasForeignKey(holding =>
                holding.FundPortfolioReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(holding => holding.MatchedMarketAsset)
            .WithMany()
            .HasForeignKey(holding =>
                holding.MatchedMarketAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
