using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MiniMola.Application.Markets;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class MarketsController(IMarketDataHealthService health) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Health(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return View(await health.GetAsync(userId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RetryHealth(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var message = await health.RetryAsync(userId, id, cancellationToken);
        if (message is null) return NotFound();
        TempData["MarketHealthMessage"] = message;
        return RedirectToAction(nameof(Health));
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Detail(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        return View(id);
    }
}
