using EventAggregator.API.Services;
using EventAggregator.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedsController : ControllerBase
{
    private readonly RssFeedService _feedService;
    private readonly AppDbContext _db;

    public FeedsController(RssFeedService feedService, AppDbContext db)
    {
        _feedService = feedService;
        _db = db;
    }

    /// <summary>Повертає список усіх RSS-джерел</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.FeedSources.ToListAsync());

    /// <summary>Оновлює всі активні стрічки</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshAll()
    {
        await _feedService.RefreshAllFeedsAsync();
        return Ok(new { message = "Усі стрічки оновлено" });
    }

    /// <summary>Оновлює одну стрічку за ID</summary>
    [HttpPost("{id:int}/refresh")]
    public async Task<IActionResult> RefreshOne(int id)
    {
        var source = await _db.FeedSources.FindAsync(id);
        if (source is null) return NotFound();

        await _feedService.RefreshFeedAsync(source);
        return Ok(new { message = $"Стрічку «{source.Name}» оновлено" });
    }
}
