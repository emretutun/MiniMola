using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Shop;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/shop/fish")]
public sealed class FishShopApiController(
    IFishShopService fishShopService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FishShopDto>> Get(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var shop = await fishShopService.GetShopAsync(
            identityUserId,
            cancellationToken);

        if (shop is null)
        {
            return NotFound();
        }

        return Ok(shop);
    }

    [HttpPost("{fishSpeciesId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<PurchaseFishResultDto>> Purchase(
        int fishSpeciesId,
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var result = await fishShopService.PurchaseFishAsync(
            identityUserId,
            fishSpeciesId,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}