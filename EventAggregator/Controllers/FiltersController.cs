using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FiltersController : ControllerBase
{
    private readonly AppDbContext _db;

    public FiltersController(AppDbContext db) => _db = db;

    /// <summary>
    /// Повертає всі фільтри. Можна відфільтрувати за типом: ?type=Artist
    /// Типи: Artist, EventType, City, Technology
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? type)
    {
        var query = _db.Filters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(f => f.Type == type);

        var filters = await query
            .OrderBy(f => f.Type)
            .ThenBy(f => f.Name)
            .Select(f => new { f.Id, f.Name, f.Type, f.SearchKeyword })
            .ToListAsync();

        return Ok(filters);
    }

    /// <summary>Створити новий фільтр</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateFilterRequest req)
    {
        var filter = new Filter
        {
            Name = req.Name,
            Type = req.Type,
            SearchKeyword = req.SearchKeyword
        };

        _db.Filters.Add(filter);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = filter.Id }, filter);
    }

    /// <summary>Видалити фільтр</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var filter = await _db.Filters.FindAsync(id);
        if (filter is null) return NotFound();

        _db.Filters.Remove(filter);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateFilterRequest(string Name, string Type, string SearchKeyword);
