using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Persistence.Seed;

internal static class DecorationItemSeed
{
    private static readonly DateTime SeedDate =
        new(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyCollection<DecorationItem> GetItems()
    {
        return
        [
            new DecorationItem
            {
                Id = 1,
                Name = "Kıvrımlı Su Bitkisi",
                Description =
                    "Akvaryuma doğal ve sakin bir görünüm kazandırır.",
                AssetKey = "curved-water-plant",
                Category = DecorationCategory.Plant,
                Price = 100,
                RequiredAquariumLevel = 1,
                DisplayScale = 1.0f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new DecorationItem
            {
                Id = 2,
                Name = "Pembe Mercan",
                Description =
                    "Akvaryumun tabanına canlı bir renk katar.",
                AssetKey = "pink-coral",
                Category = DecorationCategory.Coral,
                Price = 160,
                RequiredAquariumLevel = 1,
                DisplayScale = 0.9f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new DecorationItem
            {
                Id = 3,
                Name = "Volkan Taşı",
                Description =
                    "Balıkların çevresinde dolaşabileceği koyu renkli kaya.",
                AssetKey = "volcanic-rock",
                Category = DecorationCategory.Rock,
                Price = 140,
                RequiredAquariumLevel = 1,
                DisplayScale = 1.1f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new DecorationItem
            {
                Id = 4,
                Name = "Hazine Sandığı",
                Description =
                    "Denizin dibinde unutulmuş küçük bir hazine.",
                AssetKey = "treasure-chest",
                Category = DecorationCategory.Ornament,
                Price = 260,
                RequiredAquariumLevel = 1,
                DisplayScale = 0.85f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new DecorationItem
            {
                Id = 5,
                Name = "Mini Deniz Feneri",
                Description =
                    "Akvaryumuna kıyı kasabası havası veren özel yapı.",
                AssetKey = "mini-lighthouse",
                Category = DecorationCategory.Structure,
                Price = 500,
                RequiredAquariumLevel = 2,
                DisplayScale = 1.0f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            },
            new DecorationItem
            {
                Id = 6,
                Name = "Ay Işığı Lambası",
                Description =
                    "Akvaryuma yumuşak ve gizemli bir gece ışığı verir.",
                AssetKey = "moon-light",
                Category = DecorationCategory.Lighting,
                Price = 750,
                RequiredAquariumLevel = 3,
                DisplayScale = 1.0f,
                IsActive = true,
                CreatedAtUtc = SeedDate
            }
        ];
    }
}