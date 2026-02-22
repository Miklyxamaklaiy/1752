using Microsoft.AspNetCore.Mvc;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;
using PlechPomoshchi.Services;
using PlechPomoshchi.ViewModels;

namespace PlechPomoshchi.Controllers;

public class VolunteerController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly EmailSender _email;

    public VolunteerController(ApplicationDbContext db, EmailSender email)
    {
        _db = db;
        _email = email;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new VolunteerOrgApplicationVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(VolunteerOrgApplicationVm vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var a = new VolunteerOrgApplication
        {
            OrgName = vm.OrgName.Trim(),
            Website = string.IsNullOrWhiteSpace(vm.Website) ? null : vm.Website.Trim(),
            ContactName = vm.ContactName.Trim(),
            ContactEmail = vm.ContactEmail.Trim(),
            ContactPhone = string.IsNullOrWhiteSpace(vm.ContactPhone) ? null : vm.ContactPhone.Trim(),
            Message = vm.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.VolunteerOrgApplications.Add(a);
        await _db.SaveChangesAsync();

        await _email.TrySendVolunteerOrgApplicationAsync(a);

        TempData["ok"] = "Заявка отправлена. Спасибо!";
        return RedirectToAction(nameof(Index));
    }
}
