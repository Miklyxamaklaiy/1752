using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;
using PlechPomoshchi.Services;
using PlechPomoshchi.ViewModels;

namespace PlechPomoshchi.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly OrgParser _parser;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AdminController(ApplicationDbContext db, OrgParser parser)
    {
        _db = db;
        _parser = parser;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new AdminStatsVm
        {
            Users = await _db.Users.CountAsync(),
            Organizations = await _db.Organizations.CountAsync(),
            Requests = await _db.Requests.CountAsync(),
            VolunteerApplications = await _db.VolunteerOrgApplications.CountAsync(),
            LastParserRunUtc = await _db.ParserStates.Where(x => x.Key == "org_parser").Select(x => (DateTime?)x.LastRunUtc).FirstOrDefaultAsync()
        };
        return View(stats);
    }

    public async Task<IActionResult> Users()
    {
        var list = await _db.Users.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(int id, string role)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user == null) return NotFound();

        var allowed = new[] { "Admin", "Requester", "Volunteer" };
        if (!allowed.Contains(role)) return BadRequest();

        // нельзя “снять” роль у самого себя-админа случайно, но проще: запрещаем менять роль admin@admin.com
        if (user.Email.Equals("admin@admin.com", StringComparison.OrdinalIgnoreCase) && role != "Admin")
            return BadRequest("Нельзя изменить роль встроенного администратора.");

        user.Role = role;
        await _db.SaveChangesAsync();

        TempData["ok"] = $"Роль пользователя {user.Email} изменена на {role}.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunParser()
    {
        var added = await _parser.RunAsync();
        TempData["ok"] = $"Парсер выполнен. Добавлено организаций: {added}.";
        return RedirectToAction(nameof(Index));
    }
}
