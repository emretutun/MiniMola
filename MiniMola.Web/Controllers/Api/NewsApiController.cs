using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.News;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/news")]
public sealed class NewsApiController(
    INewsService newsService,
    ILogger<NewsApiController> logger)
    : ControllerBase
{
    [HttpGet("headlines")]
    public async Task<ActionResult<NewsFeedDto>> GetHeadlines(
        CancellationToken cancellationToken)
    {
        try
        {
            var feed = await newsService.GetHeadlinesAsync(
                cancellationToken);

            return Ok(feed);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Habertürk RSS isteği zaman aşımına uğradı.");

            return Problem(
                title: "Haber servisi zaman aşımına uğradı.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Habertürk RSS servisine ulaşılamadı.");

            return Problem(
                title: "Haber servisine şu anda ulaşılamıyor.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(
                exception,
                "Habertürk RSS içeriği okunamadı.");

            return Problem(
                title: "Haber içeriği şu anda okunamıyor.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}