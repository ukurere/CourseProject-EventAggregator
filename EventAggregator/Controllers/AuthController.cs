using EventAggregator.API.Models;
using EventAggregator.API.Services;
using EventAggregator.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using System.Security.Claims;

namespace EventAggregator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // ── Реєстрація ──────────────────────────────────────────────────────────

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Email вже зайнятий" });

        var user = new User
        {
            FirstName    = req.FirstName,
            LastName     = req.LastName,
            Email        = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { token = _jwt.Generate(user), user = MapUser(user) });
    }

    // ── Вхід ────────────────────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "Невірний email або пароль" });

        if (user.TwoFactorEnabled)
            return Ok(new { requiresTwoFactor = true, userId = user.Id });

        return Ok(new { token = _jwt.Generate(user), user = MapUser(user) });
    }

    // ── Перевірка 2FA при вході ──────────────────────────────────────────────

    [HttpPost("login/2fa")]
    public async Task<IActionResult> LoginTwoFactor(TwoFactorLoginRequest req)
    {
        var user = await _db.Users.FindAsync(req.UserId);
        if (user is null || !user.TwoFactorEnabled || user.TwoFactorSecret is null)
            return BadRequest(new { message = "2FA не налаштована" });

        var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
        var totp = new Totp(secretBytes);

        if (!totp.VerifyTotp(req.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
            return Unauthorized(new { message = "Невірний код" });

        return Ok(new { token = _jwt.Generate(user), user = MapUser(user) });
    }

    // ── Поточний користувач ──────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var id   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users
            .Include(u => u.UserFilters).ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.Id == id);

        return user is null ? NotFound() : Ok(MapUser(user));
    }

    // ── Налаштування 2FA ────────────────────────────────────────────────────

    /// <summary>Крок 1: згенерувати секрет і QR-код URI</summary>
    [Authorize]
    [HttpPost("setup-2fa")]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var id   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var base32      = Base32Encoding.ToString(secretBytes);

        user.TwoFactorSecret = base32;
        await _db.SaveChangesAsync();

        var otpauthUri =
            $"otpauth://totp/EventAggregator:{Uri.EscapeDataString(user.Email)}" +
            $"?secret={base32}&issuer=EventAggregator&algorithm=SHA1&digits=6&period=30";

        return Ok(new { otpauthUri });
    }

    /// <summary>Крок 2: підтвердити код і увімкнути 2FA</summary>
    [Authorize]
    [HttpPost("confirm-2fa")]
    public async Task<IActionResult> ConfirmTwoFactor(ConfirmTwoFactorRequest req)
    {
        var id   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);

        if (user is null || user.TwoFactorSecret is null)
            return BadRequest(new { message = "Спочатку налаштуйте 2FA" });

        var secretBytes = Base32Encoding.ToBytes(user.TwoFactorSecret);
        var totp        = new Totp(secretBytes);

        if (!totp.VerifyTotp(req.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
            return BadRequest(new { message = "Невірний код — спробуйте ще раз" });

        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "2FA увімкнено" });
    }

    /// <summary>Вимкнути 2FA</summary>
    [Authorize]
    [HttpDelete("2fa")]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var id   = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret  = null;
        await _db.SaveChangesAsync();

        return Ok(new { message = "2FA вимкнено" });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static object MapUser(User u) => new
    {
        u.Id, u.FirstName, u.LastName, u.Email,
        u.PhotoUrl, u.ReportFrequency, u.TwoFactorEnabled,
    };
}

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record TwoFactorLoginRequest(int UserId, string Code);
public record ConfirmTwoFactorRequest(string Code);
