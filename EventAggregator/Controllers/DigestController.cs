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

    /// <summary>Надіслати дайджест на пошту (кнопка «Надіслати зараз»)</summary>
    [HttpPost("send/{userId:int}")]
    public async Task<IActionResult> SendToUser(int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();

        try
        {
            var count = await _digestService.SendNowAsync(user, telegramOnly: false);
            return Ok(new { message = $"Дайджест надіслано на {user.Email} ({count} подій)" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Надіслати дайджест тільки в Telegram (кнопка «У Telegram»)</summary>
    [HttpPost("send-telegram/{userId:int}")]
    public async Task<IActionResult> SendTelegramToUser(int userId)
    {
        var user = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null) return NotFound();
        if (!user.TelegramChatId.HasValue)
            return BadRequest(new { error = "Telegram не підключено. Спочатку надішліть /start боту." });

        try
        {
            var count = await _digestService.SendNowAsync(user, telegramOnly: true);
            return Ok(new { message = $"Дайджест надіслано в Telegram ({count} подій)" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
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
