namespace MiniMola.Application.Markets;

public interface IFundPortfolioService
{
    Task<FundPortfolioDto?> GetLatestAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default);
}
