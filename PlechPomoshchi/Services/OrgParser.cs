using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PlechPomoshchi.Data;
using PlechPomoshchi.Models;

namespace PlechPomoshchi.Services;

public class OrgParser
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<OrgParser> _log;
    private readonly IConfiguration _cfg;

    public OrgParser(ApplicationDbContext db, IHttpClientFactory http, ILogger<OrgParser> log, IConfiguration cfg)
    {
        _db = db;
        _http = http;
        _log = log;
        _cfg = cfg;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var min = int.TryParse(_cfg["Parser:MinOrganizations"], out var m) ? m : 1000;
        var coolDownHours = int.TryParse(_cfg["Parser:CoolDownHours"], out var h) ? h : 168;

        var state = await _db.ParserStates.FirstOrDefaultAsync(x => x.Key == "org_parser", ct);
        if (state != null && (DateTime.UtcNow - state.LastRunUtc).TotalHours < coolDownHours)
        {
            _log.LogInformation("Parser cooldown active. Skipping run.");
            return 0;
        }

        var before = await _db.Organizations.CountAsync(ct);
        _log.LogInformation("OrgParser started. Orgs before: {before}", before);

        var queries = BuildQueries();
        var fetched = 0;

        foreach (var q in queries)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                fetched += await TryFetchFromDuckDuckGoAsync(q, ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Fetch failed for query: {q}", q);
            }
        }

        // Ensure at least min organizations exist.
        var count = await _db.Organizations.CountAsync(ct);
        if (count < min)
        {
            _log.LogInformation("Orgs below minimum ({count} < {min}). Adding demo points.", count, min);
            await AddDemoOrgsAsync(min - count, ct);
        }

        if (state == null)
        {
            state = new ParserState { Key = "org_parser" };
            _db.ParserStates.Add(state);
        }

        state.LastRunUtc = DateTime.UtcNow;
        state.LastOrgCount = await _db.Organizations.CountAsync(ct);

        await _db.SaveChangesAsync(ct);

        var after = await _db.Organizations.CountAsync(ct);
        _log.LogInformation("OrgParser finished. Added: {added}. Total: {after}", after - before, after);
        return after - before;
    }

    private List<string> BuildQueries()
    {
        // Главный тег "СВО" + добавочные
        var baseTag = "сво";
        var tags = new[]
        {
            "госпиталь", "волонтерство", "помощь", "ветеранам", "фонд",
            "психологическая помощь", "юридическая помощь", "медицинская помощь",
            "гуманитарная помощь", "помощь семьям", "реабилитация"
        };

        var queries = new List<string>();
        foreach (var t in tags)
            queries.Add($"{baseTag} {t} организация");

        // чуть больше вариативности
        queries.Add("волонтерский центр помощь участникам сво");
        queries.Add("пункт сбора гуманитарной помощи сво");
        queries.Add("помощь госпиталям сво волонтеры");
        return queries;
    }

    private async Task<int> TryFetchFromDuckDuckGoAsync(string query, CancellationToken ct)
    {
        // DuckDuckGo HTML (без API ключей)
        var url = "https://duckduckgo.com/html/?q=" + Uri.EscapeDataString(query + " Россия");

        var client = _http.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PlechPomoshchiBot/1.0; +https://localhost)");
        client.Timeout = TimeSpan.FromSeconds(20);

        var html = await client.GetStringAsync(url, ct);

        // result__a links
        var rx = new Regex("class=\\\"result__a\\\"[^>]*href=\\\"(?<url>[^\\\"]+)\\\"[^>]*>(?<title>.*?)<", RegexOptions.IgnoreCase);
        var matches = rx.Matches(html);

        var added = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in matches)
        {
            ct.ThrowIfCancellationRequested();

            var link = System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value).Trim();
            var title = StripTags(System.Net.WebUtility.HtmlDecode(m.Groups["title"].Value)).Trim();

            if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(title))
                continue;

            // normalize redirect links (duckduckgo often wraps with /l/?uddg=...)
            link = UnwrapDuckDuckGo(link);

            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
                continue;

            var key = uri.Host + uri.AbsolutePath;
            if (!seen.Add(key))
                continue;

            // already exists?
            var exists = await _db.Organizations.AnyAsync(x => x.Website == link || x.Name == title, ct);
            if (exists)
                continue;

            var org = new Organization
            {
                Name = title.Length > 240 ? title[..240] : title,
                Website = link.Length > 490 ? link[..490] : link,
                Category = GuessCategory(title),
                City = null,
                Address = null,
                IsFromParser = true
            };

            _db.Organizations.Add(org);
            added++;

            // Не делаем тяжёлый геокодинг на каждую запись, чтобы не упираться в лимиты.
            if (added <= 30)
                await TryGeocodeAsync(org, ct);
        }

        await _db.SaveChangesAsync(ct);
        return added;
    }

    private static string UnwrapDuckDuckGo(string url)
    {
        // e.g. https://duckduckgo.com/l/?uddg=<encoded>
        try
        {
            var u = new Uri(url);
            if (u.Host.Contains("duckduckgo.com") && u.AbsolutePath.StartsWith("/l/"))
            {
                var q = QueryHelpers.ParseQuery(u.Query);
                var uddg = q.TryGetValue("uddg", out var v) ? v.ToString() : null;
                if (!string.IsNullOrWhiteSpace(uddg))
                    return Uri.UnescapeDataString(uddg);
            }
        }
        catch { }
        return url;
    }

    private async Task TryGeocodeAsync(Organization org, CancellationToken ct)
    {
        try
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; PlechPomoshchiGeocoder/1.0)");
            client.Timeout = TimeSpan.FromSeconds(15);

            var q = $"{org.Name} Россия";
            var url = "https://nominatim.openstreetmap.org/search?format=json&limit=1&q=" + Uri.EscapeDataString(q);

            var json = await client.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(json);

            var arr = doc.RootElement;
            if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
                return;

            var first = arr[0];
            if (first.TryGetProperty("lat", out var latEl) && first.TryGetProperty("lon", out var lonEl))
            {
                if (double.TryParse(latEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(lonEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
                {
                    org.Lat = lat;
                    org.Lng = lon;
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Geocoding failed for {name}", org.Name);
        }
    }

    private async Task AddDemoOrgsAsync(int count, CancellationToken ct)
    {
        // Набор “реалистичных” городов РФ с координатами
        var cities = new (string City, double Lat, double Lng)[]
        {
            ("Москва", 55.7558, 37.6173),
            ("Санкт‑Петербург", 59.9311, 30.3609),
            ("Казань", 55.7961, 49.1064),
            ("Нижний Новгород", 56.2965, 43.9361),
            ("Ростов‑на‑Дону", 47.2357, 39.7015),
            ("Краснодар", 45.0355, 38.9753),
            ("Воронеж", 51.6608, 39.2003),
            ("Екатеринбург", 56.8389, 60.6057),
            ("Новосибирск", 55.0084, 82.9357),
            ("Самара", 53.1959, 50.1008),
            ("Челябинск", 55.1644, 61.4368),
            ("Уфа", 54.7388, 55.9721),
            ("Пермь", 58.0105, 56.2502),
            ("Волгоград", 48.7080, 44.5133),
            ("Саратов", 51.5331, 46.0342),
            ("Омск", 54.9885, 73.3242),
            ("Тюмень", 57.1613, 65.5250),
            ("Хабаровск", 48.4802, 135.0719),
            ("Владивосток", 43.1155, 131.8855),
        };

        var categories = new[]
        {
            "Психологическая", "Юридическая", "Финансовая",
            "Медицинская", "Гуманитарная", "Госпитали"
        };

        var rnd = new Random();
        var now = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var c = cities[rnd.Next(cities.Length)];
            var cat = categories[rnd.Next(categories.Length)];

            // небольшое случайное смещение, чтобы маркеры не слипались
            var lat = c.Lat + (rnd.NextDouble() - 0.5) * 0.25;
            var lng = c.Lng + (rnd.NextDouble() - 0.5) * 0.35;

            var org = new Organization
            {
                Name = $"{cat} помощь — {c.City} #{rnd.Next(10000, 99999)}",
                City = c.City,
                Category = cat,
                Address = $"г. {c.City}, ул. Примерная, {rnd.Next(1, 200)}",
                Website = null,
                Lat = lat,
                Lng = lng,
                IsFromParser = false,
                CreatedAt = now
            };

            _db.Organizations.Add(org);
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string GuessCategory(string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("псих") || t.Contains("психолог")) return "Психологическая";
        if (t.Contains("юрид") || t.Contains("адвокат") || t.Contains("право")) return "Юридическая";
        if (t.Contains("финанс") || t.Contains("сбор") || t.Contains("пожертв")) return "Финансовая";
        if (t.Contains("госпитал") || t.Contains("мед") || t.Contains("реабил")) return "Медицинская";
        if (t.Contains("гуманитар")) return "Гуманитарная";
        return "Помощь";
    }

    private static string StripTags(string input)
    {
        return Regex.Replace(input, "<.*?>", string.Empty);
    }
}
