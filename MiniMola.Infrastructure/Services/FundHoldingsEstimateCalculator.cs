using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Services;

public static class FundHoldingsEstimateCalculator
{
    public static FundEstimateDto Calculate(MarketAsset asset, FundPortfolioDto portfolio,
        IReadOnlyList<MarketPriceSnapshot> snapshots, DateTime nowUtc)
    {
        var today = DateOnly.FromDateTime(nowUtc.AddHours(3));
        var localTime = TimeOnly.FromDateTime(nowUtc.AddHours(3));
        var basePrice = snapshots.FirstOrDefault(x => x.MarketAssetId == asset.Id
            && x.PriceKind == MarketPriceKind.Official && x.Source == "TEFAS / BEFAS" && x.Price > 0);
        var prices = snapshots.GroupBy(x => x.MarketAssetId).ToDictionary(g => g.Key,
            g => g.OrderByDescending(x => x.ObservedAtUtc).First());
        var contributions = new List<FundEstimateContributionDto>();
        foreach (var holding in portfolio.Holdings)
        {
            var price = holding.MarketAssetId is int id ? prices.GetValueOrDefault(id) : null;
            var covered = price is { Price: > 0, DailyChangePercent: not null, Source: "Yahoo Finance" }
                && price.ObservedAtUtc <= nowUtc
                && DateOnly.FromDateTime(price.ObservedAtUtc.AddHours(3)) == today
                && (nowUtc - price.ObservedAtUtc <= TimeSpan.FromMinutes(45)
                    || (localTime >= new TimeOnly(18, 30)
                        && TimeOnly.FromDateTime(price.ObservedAtUtc.AddHours(3)) >= new TimeOnly(18, 0)));
            contributions.Add(new(holding.Symbol, holding.Name, holding.WeightPercent,
                holding.Symbol, covered ? price!.DailyChangePercent : null,
                covered ? holding.WeightPercent * price!.DailyChangePercent / 100m : null,
                price?.ObservedAtUtc, covered));
        }
        var total = portfolio.Holdings.Sum(x => x.WeightPercent);
        if (total < 100m)
            contributions.Add(new("other", "Diğer varlıklar / modellenmeyen bölüm", 100m - total,
                null, null, null, null, false));
        var coverage = contributions.Where(x => x.IsCovered).Sum(x => x.WeightPercent);
        string? reason = !portfolio.IsAvailable ? portfolio.Message ?? "KAP raporu alınamadı."
            : portfolio.ReportAgeDays is null or > 45 ? "KAP raporu 45 günden eski; güncel rapor gerekiyor."
            : total is <= 0 or > 100.5m || portfolio.Holdings.Any(x => x.WeightPercent < 0)
                ? "KAP ağırlıkları doğrulanamadı."
            : today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Hafta sonu: yeni seans tahmini bekleniyor."
            : basePrice is null ? "Son resmî fon fiyatı bekleniyor."
            : DateOnly.FromDateTime(basePrice.ObservedAtUtc) != today
                ? "Bugünün resmî fon baz fiyatı bekleniyor; eski bazla tahmin üretilmedi."
            : coverage < 60m ? $"Güncel fiyat kapsamı %{coverage:0.##}. En az %60 gerekiyor; eksik veya gecikmiş hisseler bekleniyor."
            : null;
        var available = reason is null;
        var change = decimal.Round(contributions.Sum(x => x.ContributionPercent ?? 0m), 4);
        return new FundEstimateDto(asset.Id, true, available, asset.QuoteCurrency,
            basePrice?.Price, basePrice?.ObservedAtUtc, available ? change : null,
            available ? decimal.Round(basePrice!.Price * (1m + change / 100m), 8) : null,
            decimal.Round(Math.Min(coverage, 100m), 2),
            coverage >= 85m ? "high" : coverage >= 60m ? "medium" : "insufficient",
            coverage >= 85m ? "Geniş veri kapsamı" : coverage >= 60m ? "Kısmi veri kapsamı" : "Yetersiz veri",
            nowUtc, portfolio.ReportDate,
            "KAP raporundaki hisse ağırlığı × hissenin önceki kapanışa göre günlük değişimi. " +
            "Eksik bölüm %100'e tamamlanmaz. Rapor sonrası işlemler, giderler ve diğer varlıklar bilinmiyor; " +
            "bu sonuç resmî fiyat veya al/sat tavsiyesi değildir.",
            reason ?? $"Rapor {portfolio.ReportAgeDays} günlük. Modellenmeyen bölüm %{Math.Max(0, 100m - coverage):0.##}. " +
                "Veri kapsamı, tahmin doğruluğu anlamına gelmez. Fiyatlar gecikmeli olabilir.",
            contributions.OrderByDescending(x => Math.Abs(x.ContributionPercent ?? 0m))
                .ThenByDescending(x => x.WeightPercent).ToList())
            { ModelVersion = "kap-holdings-v2" };
    }
}
