using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Aquariums;
using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using MiniMola.Web.RateLimiting;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/aquarium")]
public sealed class AquariumApiController(
    IAquariumService aquariumService,
    IValidator<UpdateFishNicknameRequest>
        fishNicknameValidator)
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
    [EnableRateLimiting(
    RateLimitingServiceExtensions.AquariumWritePolicy)]
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

    [HttpPost("fish/{userFishId:int}/feed")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(
    RateLimitingServiceExtensions.AquariumWritePolicy)]
    public async Task<ActionResult<FeedFishResultDto>>
    FeedFish(
        int userFishId,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var result =
            await aquariumService.FeedFishAsync(
                identityUserId,
                userFishId,
                cancellationToken);

        if (!result.Success)
        {
            if (result.NextFeedAtUtc.HasValue)
            {
                return Conflict(result);
            }

            return BadRequest(result);
        }

        return Ok(result);
    }


    [HttpPut("fish/{userFishId:int}/nickname")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(
    RateLimitingServiceExtensions.AquariumWritePolicy)]
    public async Task<ActionResult<UpdateFishNicknameResultDto>>
    UpdateFishNickname(
        int userFishId,
        [FromBody] UpdateFishNicknameRequest request,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }
        var validationResult =
        await fishNicknameValidator.ValidateAsync(
        request,
        cancellationToken);

        if (!validationResult.IsValid)
        {
            var problemDetails =
                new ValidationProblemDetails(
                    validationResult.ToDictionary())
                {
                    Title = "Gönderilen bilgiler doğrulanamadı.",
                    Status = StatusCodes.Status400BadRequest,
                    Instance = Request.Path
                };

            problemDetails.Extensions["traceId"] =
                Activity.Current?.Id
                ?? HttpContext.TraceIdentifier;

            return BadRequest(problemDetails);
        }

        var result =
            await aquariumService.UpdateFishNicknameAsync(
                identityUserId,
                userFishId,
                request.Nickname,
                cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

}