using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence.Seed;

namespace MiniMola.Infrastructure.Persistence.Configurations;

public sealed class FishSpeciesConfiguration
    : IEntityTypeConfiguration<FishSpecies>
{
    public void Configure(EntityTypeBuilder<FishSpecies> builder)
    {
        builder.ToTable(
            "FishSpecies",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_FishSpecies_Price_NonNegative",
                    "[Price] >= 0");

                table.HasCheckConstraint(
                    "CK_FishSpecies_BaseSpeed_Positive",
                    "[BaseSpeed] > 0");

                table.HasCheckConstraint(
                    "CK_FishSpecies_DisplayScale_Positive",
                    "[DisplayScale] > 0");

                table.HasCheckConstraint(
                    "CK_FishSpecies_RequiredLevel_Positive",
                    "[RequiredAquariumLevel] >= 1");
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

        builder.Property(x => x.Rarity)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.BaseSpeed)
            .HasDefaultValue(1.0f);

        builder.Property(x => x.DisplayScale)
            .HasDefaultValue(1.0f);

        builder.Property(x => x.RequiredAquariumLevel)
            .HasDefaultValue(1);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.AssetKey)
            .IsUnique();
        
        builder.HasData(FishSpeciesSeed.GetItems());

    }

}