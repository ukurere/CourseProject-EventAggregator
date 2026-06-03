using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    // ── Профіль ─────────────────────────────────────────────────────────────

    /// <summary>Реєстрація нового користувача</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Користувач з таким email вже існує" });

        var user = new User
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email,
            ReportFrequency = req.ReportFrequency ?? "Weekly"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapUser(user, []));
    }

    /// <summary>Профіль користувача разом з його фільтрами</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        return Ok(MapUser(user, user.UserFilters.Select(uf => uf.Filter)));
    }

    /// <summary>Оновити налаштування профілю</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.FirstName          = req.FirstName          ?? user.FirstName;
        user.LastName           = req.LastName           ?? user.LastName;
        user.PhotoUrl           = req.PhotoUrl           ?? user.PhotoUrl;
        user.ReportFrequency    = req.ReportFrequency    ?? user.ReportFrequency;
        user.PreferredLanguage  = req.PreferredLanguage  ?? user.PreferredLanguage;
        user.PreferredCategories = req.PreferredCategories ?? user.PreferredCategories;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Фільтри користувача ─────────────────────────────────────────────────

    /// <summary>Список фільтрів, на які підписаний користувач</summary>
    [HttpGet("{id:int}/filters")]
    public async Task<IActionResult> GetFilters(int id)
    {
        var exists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!exists) return NotFound();

        var filters = await _db.UserFilters
            .Where(uf => uf.UserId == id)
            .Select(uf => new { uf.Filter.Id, uf.Filter.Name, uf.Filter.Type, uf.Filter.SearchKeyword })
            .ToListAsync();

        return Ok(filters);
    }

    /// <summary>Підписатися на фільтр</summary>
    [HttpPost("{id:int}/filters/{filterId:int}")]
    public async Task<IActionResult> AddFilter(int id, int filterId)
    {
        var userExists = await _db.Users.AnyAsync(u => u.Id == id);
        if (!userExists) return NotFound(new { message = "Користувача не знайдено" });

        var filterExists = await _db.Filters.AnyAsync(f => f.Id == filterId);
        if (!filterExists) return NotFound(new { message = "Фільтр не знайдено" });

        var alreadySubscribed = await _db.UserFilters
            .AnyAsync(uf => uf.UserId == id && uf.FilterId == filterId);

        if (alreadySubscribed)
            return Conflict(new { message = "Вже підписаний на цей фільтр" });

        _db.UserFilters.Add(new UserFilter { UserId = id, FilterId = filterId });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Підписка додана" });
    }

    /// <summary>Відписатися від фільтра</summary>
    [HttpDelete("{id:int}/filters/{filterId:int}")]
    public async Task<IActionResult> RemoveFilter(int id, int filterId)
    {
        var uf = await _db.UserFilters
            .FirstOrDefaultAsync(uf => uf.UserId == id && uf.FilterId == filterId);

        if (uf is null) return NotFound();

        _db.UserFilters.Remove(uf);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Персоналізована стрічка ──────────────────────────────────────────────

    /// <summary>
    /// Персоналізована стрічка подій для користувача —
    /// повертає події, що відповідають хоча б одному з його фільтрів.
    /// </summary>
    [HttpGet("{id:int}/feed")]
    public async Task<IActionResult> GetFeed(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var keywords = await _db.UserFilters
            .Where(uf => uf.UserId == id)
            .Select(uf => uf.Filter.SearchKeyword.ToLower())
            .ToListAsync();

        if (!keywords.Any())
            return Ok(new { message = "Немає фільтрів. Додайте підписки.", total = 0, events = Array.Empty<object>() });

        var query = _db.Events
            .Include(e => e.FeedSource)
            .Where(e => keywords.Any(k => e.Title.ToLower().Contains(k) || e.Description.ToLower().Contains(k)));

        var total = await query.CountAsync();

        var events = await query
            .OrderByDescending(e => e.PublishedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                e.SourceUrl,
                e.ImageUrl,
                e.PublishedDate,
                e.EventDate,
                e.Location,
                e.Category,
                e.Author,
                Source = e.FeedSource == null ? null : new { e.FeedSource.Name, e.FeedSource.Language }
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, events });
    }

    // ── Збережені / Календар ────────────────────────────────────────────────

    /// <summary>Події, позначені як цікаві</summary>
    [HttpGet("{id:int}/saved-events")]
    public async Task<IActionResult> GetSavedEvents(int id)
    {
        var events = await _db.UserEventStatuses
            .Where(s => s.UserId == id && s.Status == "Interesting")
            .Include(s => s.Event).ThenInclude(e => e.FeedSource)
            .OrderByDescending(s => s.MarkedAt)
            .Select(s => new
            {
                s.Event.Id, s.Event.Title, s.Event.Description, s.Event.SourceUrl,
                s.Event.ImageUrl, s.Event.PublishedDate, s.Event.EventDate,
                s.Event.Location, s.Event.Category, s.Event.Author,
                Source = s.Event.FeedSource == null ? null
                    : new { s.Event.FeedSource.Name, s.Event.FeedSource.Language },
                s.MarkedAt,
            })
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>Події, додані до календаря</summary>
    [HttpGet("{id:int}/calendar-events")]
    public async Task<IActionResult> GetCalendarEvents(int id)
    {
        var events = await _db.UserEventStatuses
            .Where(s => s.UserId == id && s.IsInCalendar)
            .Include(s => s.Event).ThenInclude(e => e.FeedSource)
            .OrderBy(s => s.Event.EventDate ?? s.Event.PublishedDate)
            .Select(s => new
            {
                s.Event.Id, s.Event.Title, s.Event.Description, s.Event.SourceUrl,
                s.Event.ImageUrl, s.Event.PublishedDate, s.Event.EventDate,
                s.Event.Location, s.Event.Category, s.Event.Author,
                Source = s.Event.FeedSource == null ? null
                    : new { s.Event.FeedSource.Name, s.Event.FeedSource.Language },
            })
            .ToListAsync();
        return Ok(events);
    }

    // ── Статус події ─────────────────────────────────────────────────────────

    /// <summary>Позначити подію як "цікаво" або "до календаря"</summary>
    [HttpPost("{id:int}/events/{eventId:int}/status")]
    public async Task<IActionResult> SetEventStatus(
        int id, int eventId, SetEventStatusRequest req)
    {
        var status = await _db.UserEventStatuses
            .FirstOrDefaultAsync(s => s.UserId == id && s.EventId == eventId);

        if (status is null)
        {
            _db.UserEventStatuses.Add(new UserEventStatus
            {
                UserId = id,
                EventId = eventId,
                Status = req.Status ?? "Interesting",
                IsInCalendar = req.IsInCalendar,
                MarkedAt = DateTime.UtcNow
            });
        }
        else
        {
            status.Status = req.Status ?? status.Status;
            status.IsInCalendar = req.IsInCalendar;
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Статус оновлено" });
    }

    /// <summary>Прибрати статус події (скасувати збереження/календар)</summary>
    [HttpDelete("{id:int}/events/{eventId:int}/status")]
    public async Task<IActionResult> RemoveEventStatus(int id, int eventId)
    {
        var status = await _db.UserEventStatuses
            .FirstOrDefaultAsync(s => s.UserId == id && s.EventId == eventId);

        if (status is null) return NoContent();

        _db.UserEventStatuses.Remove(status);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    // ── Telegram ─────────────────────────────────────────────────────────────

    /// <summary>Зберегти Telegram-налаштування користувача</summary>
    [HttpPatch("{id:int}/telegram")]
    public async Task<IActionResult> UpdateTelegram(int id, UpdateTelegramRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        // Strip leading @ if user typed it
        var username = req.TelegramUsername?.Trim().TrimStart('@').ToLower();

        // If username changed — reset chat link so user needs to /start again
        if (user.TelegramUsername != username)
        {
            user.TelegramUsername = username;
            user.TelegramChatId   = null;
        }

        user.TelegramNotificationsEnabled = req.TelegramNotificationsEnabled;
        await _db.SaveChangesAsync();

        return Ok(new { user.TelegramChatId });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object MapUser(User u, IEnumerable<Filter> filters) => new
    {
        u.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        u.PhotoUrl,
        u.ReportFrequency,
        u.LastReportSent,
        u.TelegramNotificationsEnabled,
        u.TelegramUsername,
        u.TelegramChatId,
        u.PreferredLanguage,
        u.PreferredCategories,
        Filters = filters.Select(f => new { f.Id, f.Name, f.Type })
    };
}

public record CreateUserRequest(string FirstName, string LastName, string Email, string? ReportFrequency);
public record UpdateUserRequest(string? FirstName, string? LastName, string? PhotoUrl, string? ReportFrequency, string? PreferredLanguage, string? PreferredCategories);
public record UpdateTelegramRequest(bool TelegramNotificationsEnabled, string? TelegramUsername);
public record SetEventStatusRequest(string? Status, bool IsInCalendar);
