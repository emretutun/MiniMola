using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MiniMola.Application.MemoryGames;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/games/memory")]
public sealed class MemoryGameApiController(
    IMemoryGameService memoryGameService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MemoryGameStatusDto>> Get(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var status = await memoryGameService.GetStatusAsync(
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
    public async Task<ActionResult<MemoryGameResultDto>> Complete(
        [FromBody] CompleteMemoryGameRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        if (request.MatchedPairs < 0
            || request.MoveCount < 0)
        {
            return BadRequest(
                new
                {
                    message = "Oyun sonucu geçersiz."
                });
        }

        var result = await memoryGameService.CompleteAsync(
            identityUserId,
            request.MatchedPairs,
            request.MoveCount,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}