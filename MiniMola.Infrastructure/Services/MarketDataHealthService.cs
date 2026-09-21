using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Infrastructure.Persistence;

namespace MiniMola.Infrastructure.Services;

public sealed class MarketDataHealthService(ApplicationDbContext db, IMemoryCache cache,
    IMarketPriceRefreshService prices, IFundPortfolioService portfolios, ILogger<MarketDataHealthService> logger)
    : IMarketDataHealthService
{
    private static readonly object RetryGate = new();

    private IQueryable<MarketAsset> Visible(string userId) => db.MarketAssets.AsNoTracking()
        .Where(a => a.IsActive && (a.IsFeatured || db.UserFavoriteAssets.Any(f => f.MarketAssetId == a.Id
            && db.UserProfiles.Any(p => p.Id == f.UserProfileId && p.IdentityUserId == userId))));

    public async Task<IReadOnlyList<MarketDataHealthDto>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var assets = await Visible(userId).OrderBy(a => a.Symbol).Take(200).ToListAsync(cancellationToken);
        var ids = assets.Select(a => a.Id).ToArray();
        var snapshots = await db.MarketPriceSnapshots.AsNoTracking().Where(p => ids.Contains(p.MarketAssetId))
            .GroupBy(p => p.MarketAssetId).Select(g => g.OrderByDescending(p => p.ObservedAtUtc).ThenByDescending(p => p.Id).First())
            .ToListAsync(cancellationToken);
        var reports = await db.FundPortfolioReports.AsNoTracking().Where(r => ids.Contains(r.FundMarketAssetId))
            .GroupBy(r => r.FundMarketAssetId).Select(g => g.OrderByDescending(r => r.ReportDate).First()).ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
        var estimates = await db.FundEstimateSnapshots.AsNoTracking()
            .Where(e => ids.Contains(e.MarketAssetId) && e.ModelVersion == "kap-holdings-v2")
            .GroupBy(e => e.MarketAssetId).Select(g => g.OrderByDescending(e => e.CalculatedAtUtc).First()).ToListAsync(cancellationToken);
        return assets.Select(a =>
        {
            var price = snapshots.FirstOrDefault(p => p.MarketAssetId == a.Id);
            var report = reports.FirstOrDefault(r => r.FundMarketAssetId == a.Id);
            var estimate = estimates.FirstOrDefault(e => e.MarketAssetId == a.Id);
            cache.TryGetValue<FundPortfolioDto>($"kap-health:{a.Id}", out var last);
            var reportState = a.DataProviderCode != "TEFAS_YAT" ? "Bu rapor kontrolünün kapsamı dışında"
                : last is { IsSupported: false } ? "İçerik modeli kapsamı dışında"
                : report is null ? last is { IsAvailable: false } ? "Rapor alınamadı / doğrulanamadı" : "Henüz rapor kaydı yok"
                : today.DayNumber - report.ReportDate.DayNumber > 45 ? "Rapor eski (45+ gün)" : "Kayıtlı rapor var";
            return new MarketDataHealthDto(a.Id, a.Symbol, a.Name, price?.Source ?? a.DataProviderCode ?? "Sağlayıcı tanımlı değil",
                GetPriceStatus(price?.ObservedAtUtc, price?.DailyChangePercent, a.MarketCode == "CRYPTO", DateTime.UtcNow),
                price?.ObservedAtUtc, price?.CreatedAtUtc, report?.ReportDate, report?.MatchedWeightPercent, reportState, last?.Message,
                estimate?.CoveragePercent, estimate?.CalculatedAtUtc);
        }).ToList();
    }

    public static string GetPriceStatus(DateTime? observed, decimal? change, bool crypto, DateTime now)
    {
        if (observed is null) return "Fiyat yok";
        if (observed > now) return "Fiyat zamanı gelecekte; kontrol gerekiyor";
        if (now - observed > (crypto ? TimeSpan.FromMinutes(20) : TimeSpan.FromDays(4))) return "Eski fiyat; kontrol gerekiyor";
        return change is null ? "Fiyat var; günlük değişim yok" : "Fiyat kaydı var";
    }

    public async Task<string?> RetryAsync(string userId, int assetId, CancellationToken cancellationToken = default)
    {
        var asset = await Visible(userId).SingleOrDefaultAsync(a => a.Id == assetId, cancellationToken);
        if (asset is null) return null;
        lock (RetryGate)
        {
            // Shared per asset: multiple browser tabs/users cannot flood the same source.
            var key = $"market-health-retry:{assetId}";
            if (cache.TryGetValue(key, out _)) return "Bu varlık için 2 dakika bekleyip tekrar deneyebilirsin.";
            cache.Set(key, true, TimeSpan.FromMinutes(2));
        }
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            await prices.RefreshStalePricesAsync([assetId], budget.Token);
            if (asset.DataProviderCode == "TEFAS_YAT")
            {
                // Explicit user retry bypasses only the report check cache, not provider limits.
                cache.Remove($"kap-report-check:{assetId}");
                await portfolios.GetLatestAsync(assetId, budget.Token);
            }
            return "Kontrol tamamlandı. Yeni veri gelmiş olmayabilir; kayıt zamanı ve açıklamayı kontrol et. Sağlayıcı bekleme süreleri korunur.";
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return "Kontrol süre sınırına ulaştı. Son kayıtlar korundu; daha sonra yeniden deneyebilirsin."; }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Veri sağlığı yeniden denemesi başarısız: {AssetId}", assetId);
            return "Kaynak kontrolü tamamlanamadı. Son kayıtlar korundu.";
        }
    }
}
