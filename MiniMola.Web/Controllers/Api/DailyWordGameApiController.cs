using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.WordGames;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/games/word")]
public sealed class DailyWordGameApiController(
    IDailyWordGameService wordGameService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DailyWordGameDto>> GetToday(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var game = await wordGameService.GetTodayAsync(
            identityUserId,
            cancellationToken);

        if (game is null)
        {
            return NotFound(new
            {
                message =
                    "Bugün için kelime oyunu bulunamadı."
            });
        }

        return Ok(game);
    }

    [HttpPost("guess")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<SubmitWordGuessResultDto>> SubmitGuess(
        [FromBody] SubmitWordGuessRequestDto request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var result = await wordGameService.SubmitGuessAsync(
            identityUserId,
            request.Guess,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}