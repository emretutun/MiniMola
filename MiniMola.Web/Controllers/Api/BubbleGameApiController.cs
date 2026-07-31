using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.BubbleGames;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/games/bubble")]
public sealed class BubbleGameApiController(
    IBubbleGameService bubbleGameService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BubbleGameStatusDto>> Get(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var status = await bubbleGameService.GetStatusAsync(
            identityUserId,
            cancellationToken);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpPost("complete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<BubbleGameResultDto>> Complete(
        [FromBody] CompleteBubbleGameRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        if (request.Score < 0)
        {
            return BadRequest(
                new
                {
                    message = "Skor sıfırdan küçük olamaz."
                });
        }

        var result = await bubbleGameService.CompleteAsync(
            identityUserId,
            request.Score,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}