namespace MiniMola.Application.Markets;

public interface IFundPortfolioService
{
    Task RefreshReportsAsync(CancellationToken cancellationToken = default);

    Task<FundPortfolioDto?> GetLatestAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default);
}
