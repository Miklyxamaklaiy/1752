using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;
using PlechPomoshchi.ViewModels;

namespace PlechPomoshchi.Controllers;

[Authorize]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public RequestsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [Authorize(Roles = "Requester")]
    public async Task<IActionResult> My()
    {
        var uid = CurrentUserId();
        var list = await _db.Requests.AsNoTracking()
            .Where(x => x.UserId == uid)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(list);
    }

    [Authorize(Roles = "Requester")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateRequestVm());
    }

    [Authorize(Roles = "Requester")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRequestVm vm, IFormFile? file)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var uid = CurrentUserId();
        string? savedPath = null;

        if (file != null && file.Length > 0)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("", "Файл слишком большой (макс 10 МБ).");
                return View(vm);
            }

            var uploads = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);

            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            safeName = string.Concat(safeName.Where(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ));
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "file";

            var ext = Path.GetExtension(file.FileName);
            var fname = $"{safeName}-{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(uploads, fname);

            using var fs = System.IO.File.Create(full);
            await file.CopyToAsync(fs);

            savedPath = "/uploads/" + fname;
        }

        var req = new HelpRequest
        {
            UserId = uid,
            Category = vm.Category,
            Description = vm.Description.Trim(),
            Status = "Новая",
            CreatedAt = DateTime.UtcNow,
            FilePath = savedPath
        };

        _db.Requests.Add(req);
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(My));
    }

    [Authorize(Roles = "Volunteer,Admin")]
    public async Task<IActionResult> All(string? status)
    {
        var q = _db.Requests.AsNoTracking().Include(x => x.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(x => x.Status == status);

        var list = await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
        ViewBag.Status = status;
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var req = await _db.Requests
            .Include(x => x.User)
            .Include(x => x.Comments).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (req == null) return NotFound();

        if (!CanView(req))
            return Forbid();

        var vm = new RequestDetailsVm
        {
            Request = req,
            NewCommentText = ""
        };

        return View(vm);
    }

    [Authorize(Roles = "Volunteer,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id)
    {
        var req = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id);
        if (req == null) return NotFound();
        if (req.Status == "Новая") req.Status = "В работе";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Volunteer,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var req = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id);
        if (req == null) return NotFound();
        req.Status = "Выполнена";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Volunteer,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var req = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id);
        if (req == null) return NotFound();
        req.Status = "Отклонена";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, RequestDetailsVm vm)
    {
        var req = await _db.Requests.FirstOrDefaultAsync(x => x.Id == id);
        if (req == null) return NotFound();

        if (!CanView(req))
            return Forbid();

        var text = (vm.NewCommentText ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return RedirectToAction(nameof(Details), new { id });

        var uid = CurrentUserId();
        _db.Comments.Add(new RequestComment
        {
            RequestId = id,
            UserId = uid,
            Text = text,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    private int CurrentUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    private bool IsVolunteerOrAdmin()
        => User.IsInRole("Volunteer") || User.IsInRole("Admin");

    private bool CanView(HelpRequest req)
        => IsVolunteerOrAdmin() || req.UserId == CurrentUserId();
}
