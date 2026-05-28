using EventAggregator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    // ── RSS ───────────────────────────────────────────────────────────────────

    [HttpPatch("feeds/{id}/toggle")]
    public async Task<IActionResult> ToggleFeed(int id)
    {
        var source = await _db.FeedSources.FindAsync(id);
        if (source is null) return NotFound();
        source.IsActive = !source.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { source.Id, source.IsActive });
    }

    /// <summary>Загальна статистика + дані по кожному джерелу</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalEvents   = await _db.Events.CountAsync();
        var totalUsers    = await _db.Users.CountAsync();
        var activeSources = await _db.FeedSources.CountAsync(s => s.IsActive);

        var sources = await _db.FeedSources
            .Select(s => new
            {
                s.Id, s.Name, s.Category, s.Language, s.IsActive, s.LastFetched,
                EventCount = s.Events.Count(),
            })
            .OrderByDescending(s => s.EventCount)
            .ToListAsync();

        return Ok(new { totalEvents, totalUsers, activeSources, sources });
    }

    // ── Users ─────────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id, u.FirstName, u.LastName, u.Email,
                u.IsBanned, u.ReportFrequency,
                u.TelegramNotificationsEnabled, u.TelegramChatId,
                FilterCount      = u.UserFilters.Count(),
                SavedCount       = u.UserEventStatuses.Count(s => s.Status == "Interesting"),
                CalendarCount    = u.UserEventStatuses.Count(s => s.IsInCalendar),
            })
            .OrderBy(u => u.Id)
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters).ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        return Ok(new
        {
            user.Id, user.FirstName, user.LastName, user.Email,
            user.IsBanned, user.ReportFrequency, user.LastReportSent,
            user.TwoFactorEnabled,
            user.TelegramNotificationsEnabled, user.TelegramUsername, user.TelegramChatId,
            Filters       = user.UserFilters.Select(uf => new { uf.Filter.Id, uf.Filter.Name, uf.Filter.Type }),
            SavedCount    = await _db.UserEventStatuses.CountAsync(s => s.UserId == id && s.Status == "Interesting"),
            CalendarCount = await _db.UserEventStatuses.CountAsync(s => s.UserId == id && s.IsInCalendar),
        });
    }

    [HttpPost("users/{id}/toggle-ban")]
    public async Task<IActionResult> ToggleBan(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.IsBanned = !user.IsBanned;
        await _db.SaveChangesAsync();
        return Ok(new { user.Id, user.IsBanned });
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
            .Include(u => u.UserEventStatuses)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        _db.UserFilters.RemoveRange(user.UserFilters);
        _db.UserEventStatuses.RemoveRange(user.UserEventStatuses);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
