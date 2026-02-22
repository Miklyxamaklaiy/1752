using Microsoft.AspNetCore.Mvc;

namespace PlechPomoshchi.Controllers;

public class ContactsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
