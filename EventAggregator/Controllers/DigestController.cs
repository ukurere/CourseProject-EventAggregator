using EventAggregator.API.Services;
using EventAggregator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DigestController : ControllerBase
{
    private readonly DigestService _digestService;
    private readonly AppDbContext _db;

    public DigestController(DigestService digestService, AppDbContext db)
    {
        _digestService = digestService;
        _db = db;
    }

    /// <summary>Надіслати дайджести всім користувачам, яким прийшов час</summary>
    [HttpPost("send-all")]
    public async Task<IActionResult> SendAll()
    {
        await _digestService.SendAllDueAsync();
        return Ok(new { message = "Дайджести оброблено" });
    }

    /// <summary>Примусово надіслати дайджест конкретному користувачу (для тестування)</summary>
    [HttpPost("send/{userId:int}")]
    public async Task<IActionResult> SendToUser(int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        await _digestService.SendDigestAsync(user);
        return Ok(new { message = $"Дайджест надіслано на {user.Email}" });
    }

    /// <summary>Переглянути HTML-дайджест у браузері без надсилання</summary>
    [HttpGet("preview/{userId:int}")]
    public async Task<IActionResult> Preview(int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        var html = await _digestService.BuildPreviewAsync(user);
        return Content(html, "text/html; charset=utf-8");
    }
}
