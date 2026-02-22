using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;

namespace PlechPomoshchi.Controllers;

[ApiController]
[Route("api/favorites")]
public class FavoritesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public FavoritesApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [Authorize]
    [HttpPost("toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromForm] int organizationId)
    {
        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var existing = await _db.FavoriteOrganizations
            .FirstOrDefaultAsync(x => x.UserId == uid && x.OrganizationId == organizationId);

        if (existing != null)
        {
            _db.FavoriteOrganizations.Remove(existing);
            await _db.SaveChangesAsync();
            return Ok(new { favorited = false });
        }

        _db.FavoriteOrganizations.Add(new UserFavoriteOrganization
        {
            UserId = uid,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { favorited = true });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var ids = await _db.FavoriteOrganizations.AsNoTracking()
            .Where(x => x.UserId == uid)
            .Select(x => x.OrganizationId)
            .ToListAsync();

        return Ok(ids);
    }
}
