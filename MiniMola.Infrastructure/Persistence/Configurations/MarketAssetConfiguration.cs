using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class MarketAssetConfiguration
    : IEntityTypeConfiguration<MarketAsset>
{
    public void Configure(
        EntityTypeBuilder<MarketAsset> builder)
    {
        builder.ToTable(
            "MarketAssets",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_MarketAssets_AssetType",
                    "[AssetType] >= 1 AND [AssetType] <= 10");

                table.HasCheckConstraint(
                    "CK_MarketAssets_ProviderMapping",
                    "([DataProviderCode] IS NULL "
                    + "AND [ProviderSymbol] IS NULL) OR "
                    + "([DataProviderCode] IS NOT NULL "
                    + "AND [ProviderSymbol] IS NOT NULL)");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AssetType)
            .IsRequired();

        builder.Property(x => x.MarketCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.QuoteCurrency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.DataProviderCode)
            .HasMaxLength(30);

        builder.Property(x => x.ProviderSymbol)
            .HasMaxLength(100);

        builder.Property(x => x.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(
                x => new
                {
                    x.MarketCode,
                    x.Symbol,
                    x.QuoteCurrency
                });

        builder.HasIndex(
                x => new
                {
                    x.DataProviderCode,
                    x.ProviderSymbol,
                    x.QuoteCurrency
                })
            .IsUnique()
            .HasFilter(
                "[DataProviderCode] IS NOT NULL "
                + "AND [ProviderSymbol] IS NOT NULL");

        builder.HasIndex(
            x => new
            {
                x.AssetType,
                x.IsActive
            });

        builder.HasIndex(
            x => new
            {
                x.IsActive,
                x.IsFeatured
            });

        var seedCreatedAtUtc = new DateTime(
    2026,
    8,
    31,
    0,
    0,
    0,
    DateTimeKind.Utc);

        builder.HasData(
            new MarketAsset
            {
                Id = 1,
                Symbol = "BTC",
                Name = "Bitcoin",
                AssetType = MarketAssetType.CryptoCurrency,
                MarketCode = "CRYPTO",
                QuoteCurrency = "TRY",
                DataProviderCode = "COINGECKO",
                ProviderSymbol = "bitcoin",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 2,
                Symbol = "ETH",
                Name = "Ethereum",
                AssetType = MarketAssetType.CryptoCurrency,
                MarketCode = "CRYPTO",
                QuoteCurrency = "TRY",
                DataProviderCode = "COINGECKO",
                ProviderSymbol = "ethereum",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 3,
                Symbol = "XU100",
                Name = "BIST 100",
                AssetType = MarketAssetType.Index,
                MarketCode = "BIST",
                QuoteCurrency = "TRY",
                DataProviderCode = "YAHOO_FINANCE",
                ProviderSymbol = "XU100.IS",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 4,
                Symbol = "XU030",
                Name = "BIST 30",
                AssetType = MarketAssetType.Index,
                MarketCode = "BIST",
                QuoteCurrency = "TRY",
                DataProviderCode = "YAHOO_FINANCE",
                ProviderSymbol = "XU030.IS",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 5,
                Symbol = "GRAM_ALTIN",
                Name = "Gram Altın",
                AssetType = MarketAssetType.PreciousMetal,
                MarketCode = "PRECIOUS_METAL",
                QuoteCurrency = "TRY",
                DataProviderCode = "COINGECKO",
                ProviderSymbol = "pax-gold",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 6,
                Symbol = "USDTRY",
                Name = "ABD Doları / Türk Lirası",
                AssetType = MarketAssetType.ForeignExchange,
                MarketCode = "TCMB",
                QuoteCurrency = "TRY",
                DataProviderCode = "TCMB_XML",
                ProviderSymbol = "USD",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            },
            new MarketAsset
            {
                Id = 7,
                Symbol = "EURTRY",
                Name = "Euro / Türk Lirası",
                AssetType = MarketAssetType.ForeignExchange,
                MarketCode = "TCMB",
                QuoteCurrency = "TRY",
                DataProviderCode = "TCMB_XML",
                ProviderSymbol = "EUR",
                IsFeatured = true,
                IsActive = true,
                CreatedAtUtc = seedCreatedAtUtc
            });
    }
}
