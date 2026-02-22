using Microsoft.AspNetCore.Mvc;
using PlechPomoshchi.ViewModels;

namespace PlechPomoshchi.Controllers;

public class ArticlesController : Controller
{
    public IActionResult Index()
    {
        return View(ArticleCatalog.All);
    }

    public IActionResult Details(int id)
    {
        var a = ArticleCatalog.All.FirstOrDefault(x => x.Id == id);
        if (a == null) return NotFound();
        return View(a);
    }
}
