using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;

namespace PlechPomoshchi.Services;

public class Seeder
{
    private readonly ApplicationDbContext _db;
    private readonly OrgParser _parser;
    private readonly IConfiguration _cfg;
    private readonly ILogger<Seeder> _log;

    public Seeder(ApplicationDbContext db, OrgParser parser, IConfiguration cfg, ILogger<Seeder> log)
    {
        _db = db;
        _parser = parser;
        _cfg = cfg;
        _log = log;
    }

    public async Task SeedAsync()
    {
        // Учебный режим: создаём БД автоматически как файл.
        await _db.Database.EnsureCreatedAsync();

        await SeedUsersAsync();
        await SeedRequestsDemoAsync();
        await SeedOrganizationsAsync();
    }

    private async Task SeedUsersAsync()
    {
        if (await _db.Users.AnyAsync()) return;

        var hasher = new PasswordHasher<AppUser>();

        var admin = new AppUser
        {
            Email = "admin@admin.com",
            FullName = "Администратор",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = hasher.HashPassword(admin, "admin");

        var volunteer = new AppUser
        {
            Email = "volunteer@demo.com",
            FullName = "Волонтёр (демо)",
            Role = "Volunteer",
            CreatedAt = DateTime.UtcNow
        };
        volunteer.PasswordHash = hasher.HashPassword(volunteer, "demo12345");

        var requester = new AppUser
        {
            Email = "requester@demo.com",
            FullName = "Заявитель (демо)",
            Role = "Requester",
            CreatedAt = DateTime.UtcNow
        };
        requester.PasswordHash = hasher.HashPassword(requester, "demo12345");

        _db.Users.AddRange(admin, volunteer, requester);
        await _db.SaveChangesAsync();
    }

    private async Task SeedRequestsDemoAsync()
    {
        if (await _db.Requests.AnyAsync()) return;

        var requester = await _db.Users.FirstAsync(x => x.Role == "Requester");
        _db.Requests.Add(new HelpRequest
        {
            UserId = requester.Id,
            Category = "Медицинская",
            Description = "Нужна консультация по реабилитации. (Демо-заявка)",
            Status = "Новая",
            CreatedAt = DateTime.UtcNow
        });

        _db.Requests.Add(new HelpRequest
        {
            UserId = requester.Id,
            Category = "Юридическая",
            Description = "Помощь с документами/льготами. (Демо-заявка)",
            Status = "В работе",
            CreatedAt = DateTime.UtcNow.AddHours(-6)
        });

        await _db.SaveChangesAsync();
    }

    private async Task SeedOrganizationsAsync()
    {
        var min = int.TryParse(_cfg["Parser:MinOrganizations"], out var m) ? m : 1000;

        var count = await _db.Organizations.CountAsync();
        if (count >= min) return;

        // Сразу делаем демо-наполнение до 1000, чтобы приложение выглядело “живым”
        // Парсер запускается вручную из админки, и обновляет/добавляет реальные источники.
        _log.LogInformation("Seeding demo organizations to reach minimum: {min}", min);

        // Use parser demo filler (it adds demo points if not enough)
        await _parser.RunAsync();
    }
}
