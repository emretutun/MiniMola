namespace MiniMola.Application.Markets;

public interface IMarketTechnicalAnalysisService
{
    Task<MarketTechnicalAnalysisDto?> GetAnalysisAsync(
        int marketAssetId,
        CancellationToken cancellationToken = default);
}
