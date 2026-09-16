using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniMola.Application.Markets;
using MiniMola.Domain.Entities;
using MiniMola.Domain.Enums;

namespace MiniMola.Infrastructure.Services;

public sealed partial class FundEstimateService
{
    private const string HoldingsModelVersion = "kap-holdings-v2";

    private async Task<FundEstimateDto> GetHoldingsEstimateAsync(
        MarketAsset asset, FundPortfolioDto portfolio, CancellationToken cancellationToken)
    {
        var ids = portfolio.Holdings.Where(x => x.MarketAssetId.HasValue)
            .Select(x => x.MarketAssetId!.Value).Append(asset.Id).Distinct().ToArray();
        using var refreshBudget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        refreshBudget.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            await marketPriceRefreshService.RefreshStalePricesAsync(ids, refreshBudget.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("KAP tahmini fiyat yenilemesi süre sınırına ulaştı; kayıtlı veriler değerlendirilecek.");
        }
        var snapshots = await dbContext.MarketPriceSnapshots.AsNoTracking()
            .Where(x => ids.Contains(x.MarketAssetId)
                && (x.MarketAssetId != asset.Id
                    || (x.PriceKind == MarketPriceKind.Official && x.Source == OfficialFundPriceSource)))
            .GroupBy(x => x.MarketAssetId)
            .Select(g => g.OrderByDescending(x => x.ObservedAtUtc).ThenByDescending(x => x.Id).First())
            .ToListAsync(cancellationToken);
        var result = FundHoldingsEstimateCalculator.Calculate(asset, portfolio, snapshots, DateTime.UtcNow);
        await SaveEstimateAsync(asset, result, cancellationToken);
        return result;
    }
}
