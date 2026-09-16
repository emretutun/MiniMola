using System.Diagnostics;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.WorkSchedules;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/work-schedule")]
public sealed class WorkScheduleApiController(
    IWorkScheduleService workScheduleService,
    IValidator<UpdateWorkScheduleRequest> validator)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WorkScheduleDto>> Get(
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

        var schedule =
            await workScheduleService.GetAsync(
                identityUserId,
                cancellationToken);

        if (schedule is null)
        {
            return NotFound();
        }

        return Ok(schedule);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<
        ActionResult<UpdateWorkScheduleResultDto>>
        Update(
            [FromBody] UpdateWorkScheduleRequest request,
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

        var validationResult =
            await validator.ValidateAsync(
                request,
                cancellationToken);

        if (!validationResult.IsValid)
        {
            var problemDetails =
                new ValidationProblemDetails(
                    validationResult.ToDictionary())
                {
                    Title =
                        "Çalışma saatleri doğrulanamadı.",

                    Status =
                        StatusCodes.Status400BadRequest,

                    Instance = Request.Path
                };

            problemDetails.Extensions["traceId"] =
                Activity.Current?.Id
                ?? HttpContext.TraceIdentifier;

            return BadRequest(problemDetails);
        }

        var result =
            await workScheduleService.UpdateAsync(
                identityUserId,
                request,
                cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("countdown")]
    public async Task<ActionResult<WorkCountdownDto>>
        GetCountdown(
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

        var countdown =
            await workScheduleService
                .GetCountdownAsync(
                    identityUserId,
                    cancellationToken);

        if (countdown is null)
        {
            return NotFound();
        }

        return Ok(countdown);
    }
}