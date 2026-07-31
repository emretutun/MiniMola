using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MiniMola.Web.Controllers;

[Authorize]
public sealed class GamesController : Controller
{
    [HttpGet]
    public IActionResult Word()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Bubble()
    {
        return View();
    }
    
    [HttpGet]
    public IActionResult Memory()
    {
        return View();
    }

}