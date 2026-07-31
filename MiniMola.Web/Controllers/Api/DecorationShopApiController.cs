using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniMola.Application.Shop;

namespace MiniMola.Web.Controllers.Api;

[ApiController]
[Authorize]
[Route("api/shop/decorations")]
public sealed class DecorationShopApiController(
    IDecorationShopService decorationShopService)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DecorationShopDto>> Get(
        CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var shop = await decorationShopService.GetShopAsync(
            identityUserId,
            cancellationToken);

        if (shop is null)
        {
            return NotFound();
        }

        return Ok(shop);
    }

    [HttpPost("{decorationItemId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<PurchaseDecorationResultDto>>
        Purchase(
            int decorationItemId,
            CancellationToken cancellationToken)
    {
        var identityUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            return Unauthorized();
        }

        var result =
            await decorationShopService.PurchaseAsync(
                identityUserId,
                decorationItemId,
                cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}