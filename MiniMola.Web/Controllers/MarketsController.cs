using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class MarketsController : Controller
{
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
