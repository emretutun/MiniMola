using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Markets;
using MiniMola.Domain.Enums;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/markets")]
public sealed class MarketApiController(
    IMarketWatchlistService marketWatchlistService,
    IMarketPriceRefreshService marketPriceRefreshService,
    IMarketHistoryService marketHistoryService,
    IMarketTechnicalAnalysisService technicalAnalysisService,
    IFundEstimateService fundEstimateService,
    IFundPortfolioService fundPortfolioService,
    Hangfire.IBackgroundJobClient backgroundJobClient)
    : ControllerBase
{
    [HttpGet]
    public async Task<
        ActionResult<
            PagedResult<MarketAssetListItemDto>>>
        Get(
            [FromQuery] MarketAssetType? assetType,
            [FromQuery] string? search,
            [FromQuery] bool favoritesOnly = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30,
            CancellationToken cancellationToken = default)
    {
        var identityUserId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                identityUserId))
        {
            return Unauthorized();
        }

        var query =
            new MarketAssetQuery(
                assetType,
                search,
                favoritesOnly,
                page,
                pageSize);

        var result =
            await marketWatchlistService
                .GetAssetsAsync(
                    identityUserId,
                    query,
                    cancellationToken);

        var visibleAssetIds =
            result.Items
                .Select(asset => asset.Id)
                .ToArray();

        if (visibleAssetIds.Length > 0)
        {
            await marketPriceRefreshService
                .RefreshStalePricesAsync(
                    visibleAssetIds,
                    cancellationToken);

            result =
                await marketWatchlistService
                    .GetAssetsAsync(
                        identityUserId,
                        query,
                        cancellationToken);
        }

        return Ok(result);
    }

    [HttpGet("{marketAssetId:int:min(1)}/history")]
    public async Task<ActionResult<
        MarketHistoryResultDto>> GetHistory(
            int marketAssetId,
            [FromQuery] string? range = "1m",
            CancellationToken cancellationToken = default)
    {
        var result =
            await marketHistoryService.GetHistoryAsync(
                marketAssetId,
                range,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{marketAssetId:int:min(1)}/analysis")]
    public async Task<ActionResult<
        MarketTechnicalAnalysisDto>> GetAnalysis(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await technicalAnalysisService.GetAnalysisAsync(
                marketAssetId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{marketAssetId:int:min(1)}/fund-estimate")]
    public async Task<ActionResult<FundEstimateDto>>
        GetFundEstimate(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await fundEstimateService.GetEstimateAsync(
                marketAssetId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{marketAssetId:int:min(1)}/fund-estimate-history")]
    public async Task<ActionResult<FundEstimateHistoryDto>>
        GetFundEstimateHistory(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await fundEstimateService.GetHistoryAsync(
                marketAssetId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("{marketAssetId:int:min(1)}/fund-portfolio")]
    public async Task<ActionResult<FundPortfolioDto>>
        GetFundPortfolio(
            int marketAssetId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await fundPortfolioService.GetLatestAsync(
                marketAssetId,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost("{marketAssetId:int:min(1)}/favorite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFavorite(
    int marketAssetId,
    CancellationToken cancellationToken)
    {
        var identityUserId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                identityUserId))
        {
            return Unauthorized();
        }

        var added =
            await marketWatchlistService
                .AddFavoriteAsync(
                    identityUserId,
                    marketAssetId,
                    cancellationToken);

        if (!added)
        {
            return NotFound();
        }

        Hangfire.BackgroundJobClientExtensions
            .Enqueue<IMarketPriceRefreshService>(
                backgroundJobClient,
                service =>
                    service.RefreshStalePricesAsync(
                        new[] { marketAssetId },
                        CancellationToken.None));

        return NoContent();
    }

    [HttpDelete("{marketAssetId:int:min(1)}/favorite")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFavorite(
    int marketAssetId,
    CancellationToken cancellationToken)
    {
        var identityUserId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                identityUserId))
        {
            return Unauthorized();
        }

        var removed =
            await marketWatchlistService
                .RemoveFavoriteAsync(
                    identityUserId,
                    marketAssetId,
                    cancellationToken);

        if (!removed)
        {
            return NotFound();
        }

        return NoContent();
    }

}

