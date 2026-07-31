using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence.Seed;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class DecorationItemConfiguration
    : IEntityTypeConfiguration<DecorationItem>
{
    public void Configure(EntityTypeBuilder<DecorationItem> builder)
    {
        builder.ToTable(
            "DecorationItems",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_DecorationItems_Price_NonNegative",
                    "[Price] >= 0");

                table.HasCheckConstraint(
                    "CK_DecorationItems_RequiredLevel_Positive",
                    "[RequiredAquariumLevel] >= 1");

                table.HasCheckConstraint(
                    "CK_DecorationItems_DisplayScale_Positive",
                    "[DisplayScale] > 0");
            });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.AssetKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.RequiredAquariumLevel)
            .HasDefaultValue(1);

        builder.Property(x => x.DisplayScale)
            .HasDefaultValue(1.0f);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.AssetKey)
            .IsUnique();

        builder.HasData(
    DecorationItemSeed.GetItems());
    }
}