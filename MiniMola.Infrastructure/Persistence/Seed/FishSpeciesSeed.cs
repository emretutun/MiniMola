using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Persistence.Seed;

internal static class FishSpeciesSeed
{
    private static readonly DateTime SeedDate =
        new(2026, 7, 28, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyCollection<FishSpecies> GetItems()
    {
        return
        [
            new FishSpecies
            {
                Id = 1,
                Name = "Mavi Tang",
                Description = "Her yeni akvaryumun neşeli başlangıç balığı.",
                AssetKey = "blue-tang",
                Price = 0,
                Rarity = FishRarity.Common,
                BaseSpeed = 0.85f,
                DisplayScale = 0.90f,
                RequiredAquariumLevel = 1,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new FishSpecies
            {
                Id = 2,
                Name = "Palyaço Balığı",
                Description = "Turuncu ve beyaz çizgileriyle akvaryumun neşeli yüzü.",
                AssetKey = "clown-fish",
                Price = 200,
                Rarity = FishRarity.Common,
                BaseSpeed = 1.0f,
                DisplayScale = 0.85f,
                RequiredAquariumLevel = 1,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new FishSpecies
            {
                Id = 3,
                Name = "Neon Tetra",
                Description = "Parlak renkleriyle akvaryumda ışık gibi süzülür.",
                AssetKey = "neon-tetra",
                Price = 450,
                Rarity = FishRarity.Uncommon,
                BaseSpeed = 1.25f,
                DisplayScale = 0.65f,
                RequiredAquariumLevel = 1,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new FishSpecies
            {
                Id = 4,
                Name = "Beta Balığı",
                Description = "Gösterişli yüzgeçleriyle sakin ve zarif bir balık.",
                AssetKey = "betta-fish",
                Price = 800,
                Rarity = FishRarity.Rare,
                BaseSpeed = 0.70f,
                DisplayScale = 1.0f,
                RequiredAquariumLevel = 2,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new FishSpecies
            {
                Id = 5,
                Name = "Altın Balık",
                Description = "Akvaryumuna sıcaklık ve şans getiren özel balık.",
                AssetKey = "goldfish",
                Price = 1200,
                Rarity = FishRarity.Rare,
                BaseSpeed = 0.80f,
                DisplayScale = 1.05f,
                RequiredAquariumLevel = 2,
                IsActive = true,
                CreatedAtUtc = SeedDate
            }
        ];
    }
}