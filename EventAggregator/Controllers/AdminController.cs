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

    /// <summary>Загальна статистика + дані по кожному джерелу</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalEvents  = await _db.Events.CountAsync();
        var totalUsers   = await _db.Users.CountAsync();
        var activeSources = await _db.FeedSources.CountAsync(s => s.IsActive);

        var sources = await _db.FeedSources
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Category,
                s.Language,
                s.IsActive,
                s.LastFetched,
                EventCount = s.Events.Count(),
            })
            .OrderByDescending(s => s.EventCount)
            .ToListAsync();

        return Ok(new { totalEvents, totalUsers, activeSources, sources });
    }
}
