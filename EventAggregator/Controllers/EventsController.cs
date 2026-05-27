using EventAggregator.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EventsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Повертає список подій з пагінацією та фільтрацією.
    /// ?category=events  ?search=концерт  ?page=1  ?pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? category,
        [FromQuery] string? language,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Events
            .Include(e => e.FeedSource)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(language))
            query = query.Where(e => e.FeedSource != null && e.FeedSource.Language == language);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e =>
                e.Title.Contains(search) || e.Description.Contains(search));

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

    /// <summary>Повертає деталі однієї події</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var evt = await _db.Events
            .Include(e => e.FeedSource)
            .FirstOrDefaultAsync(e => e.Id == id);

        return evt is null ? NotFound() : Ok(evt);
    }
}
