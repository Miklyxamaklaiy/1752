using Microsoft.AspNetCore.Mvc;

namespace PlechPomoshchi.Controllers;

public class StoriesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
