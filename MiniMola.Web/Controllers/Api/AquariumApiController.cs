using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Aquariums;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/aquarium")]
public sealed class AquariumApiController(
    IAquariumService aquariumService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AquariumDetailsDto>> Get(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var aquarium =
            await aquariumService.GetByIdentityUserIdAsync(
                identityUserId,
                cancellationToken);

        if (aquarium is null)
        {
            return NotFound();
        }

        return Ok(aquarium);
    }

    [HttpGet("upgrade")]
    public async Task<ActionResult<AquariumUpgradeDto>>
        GetUpgradeStatus(
            CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var status =
            await aquariumService.GetUpgradeStatusAsync(
                identityUserId,
                cancellationToken);

        if (status is null)
        {
            return NotFound();
        }

        return Ok(status);
    }

    [HttpPost("upgrade")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AquariumUpgradeDto>> Upgrade(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var currentStatus =
            await aquariumService.GetUpgradeStatusAsync(
                identityUserId,
                cancellationToken);

        if (currentStatus is null)
        {
            return NotFound();
        }

        if (!currentStatus.CanUpgrade)
        {
            return BadRequest(currentStatus);
        }

        var result = await aquariumService.UpgradeAsync(
            identityUserId,
            cancellationToken);

        if (result.Level <= currentStatus.Level)
        {
            return BadRequest(result);
        }

        return Ok(result);

    }

    [HttpPut("decorations/{userDecorationId:int}/position")]
    [ValidateAntiForgeryToken]
    public async Task<
    ActionResult<UpdateDecorationPositionResultDto>>
    UpdateDecorationPosition(
        int userDecorationId,
        [FromBody] UpdateDecorationPositionRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var result =
            await aquariumService.UpdateDecorationPositionAsync(
                identityUserId,
                userDecorationId,
                request.PositionX,
                request.PositionY,
                cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}