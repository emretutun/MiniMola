namespace MiniMola.Application.Markets;

public interface IFundEstimateService
{
    Task<FundEstimateDto?> GetEstimateAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default);

    Task<FundEstimateHistoryDto?> GetHistoryAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default);

    Task EvaluatePendingAsync(
        CancellationToken cancellationToken = default);

    Task CaptureClosingEstimatesAsync(CancellationToken cancellationToken = default);
}
