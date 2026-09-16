using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class MarketPriceSnapshotConfiguration
    : IEntityTypeConfiguration<MarketPriceSnapshot>
{
    public void Configure(
        EntityTypeBuilder<MarketPriceSnapshot> builder)
    {
        builder.ToTable(
            "MarketPriceSnapshots",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_MarketPriceSnapshots_Price",
                    "[Price] > 0");

                table.HasCheckConstraint(
                    "CK_MarketPriceSnapshots_DailyChangePercent",
                    "[DailyChangePercent] IS NULL OR " +
                    "[DailyChangePercent] >= -100");

                table.HasCheckConstraint(
                    "CK_MarketPriceSnapshots_PriceKind",
                    "[PriceKind] >= 1 AND [PriceKind] <= 4");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price)
            .HasPrecision(28, 12)
            .IsRequired();

        builder.Property(x => x.DailyChangePercent)
            .HasPrecision(18, 6);

        builder.Property(x => x.PriceKind)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.ObservedAtUtc)
            .IsRequired();

        builder.HasIndex(
                x => new
                {
                    x.MarketAssetId,
                    x.ObservedAtUtc,
                    x.PriceKind,
                    x.Source
                })
            .IsUnique();

        builder.HasOne(x => x.MarketAsset)
            .WithMany()
            .HasForeignKey(x => x.MarketAssetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}