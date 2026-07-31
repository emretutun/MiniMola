using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class AquariumController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}