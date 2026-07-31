using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class ShopController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {

        return View();
    }

    [HttpGet]
    public IActionResult Decorations()
    {
        return View();
    }
}