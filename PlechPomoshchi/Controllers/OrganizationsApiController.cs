using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;

namespace PlechPomoshchi.Controllers;

[ApiController]
[Route("api/organizations")]
public class OrganizationsApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OrganizationsApiController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? category, [FromQuery] bool favoritesOnly = false)
    {
        var query = _db.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) || (x.Address != null && x.Address.ToLower().Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim().ToLower();
            query = query.Where(x => x.City != null && x.City.ToLower() == c);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(x => x.Category.ToLower() == cat);
        }

        // favoritesOnly работает только для авторизованных
        if (favoritesOnly && User.Identity?.IsAuthenticated == true)
        {
            var uid = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var favIds = _db.FavoriteOrganizations.Where(f => f.UserId == uid).Select(f => f.OrganizationId);
            query = query.Where(x => favIds.Contains(x.Id));
        }

        var list = await query
            .OrderBy(x => x.Name)
            .Take(2000) // чтобы не забивать клиент
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Address,
                x.City,
                x.Category,
                x.Website,
                x.Lat,
                x.Lng
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("cities")]
    public async Task<IActionResult> Cities()
    {
        var cities = await _db.Organizations.AsNoTracking()
            .Where(x => x.City != null && x.City != "")
            .Select(x => x.City!)
            .Distinct()
            .OrderBy(x => x)
            .Take(300)
            .ToListAsync();

        return Ok(cities);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories()
    {
        var cats = await _db.Organizations.AsNoTracking()
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return Ok(cats);
    }
}
