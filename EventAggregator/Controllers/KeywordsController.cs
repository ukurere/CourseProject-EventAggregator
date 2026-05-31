using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/users/{userId:int}/keywords")]
[Authorize]
public class KeywordsController : ControllerBase
{
    private readonly AppDbContext _db;
    public KeywordsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetKeywords(int userId)
    {
        var keywords = await _db.UserKeywords
            .Where(k => k.UserId == userId)
            .OrderBy(k => k.Keyword)
            .Select(k => new { k.Id, k.Keyword })
            .ToListAsync();
        return Ok(keywords);
    }

    [HttpPost]
    public async Task<IActionResult> AddKeyword(int userId, [FromBody] AddKeywordRequest req)
    {
        var keyword = req.Keyword.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest(new { error = "Ключове слово не може бути порожнім" });

        var exists = await _db.UserKeywords
            .AnyAsync(k => k.UserId == userId && k.Keyword == keyword);
        if (exists)
            return Conflict(new { error = "Таке ключове слово вже є" });

        var entry = new UserKeyword { UserId = userId, Keyword = keyword };
        _db.UserKeywords.Add(entry);
        await _db.SaveChangesAsync();
        return Ok(new { entry.Id, entry.Keyword });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteKeyword(int userId, int id)
    {
        var kw = await _db.UserKeywords.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId);
        if (kw is null) return NotFound();
        _db.UserKeywords.Remove(kw);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record AddKeywordRequest(string Keyword);
