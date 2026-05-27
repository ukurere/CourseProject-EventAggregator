using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EventAggregator.API.Services;

public class DigestService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<DigestService> _logger;

    public DigestService(AppDbContext db, IEmailService emailService, ILogger<DigestService> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAllDueAsync()
    {
        var now = DateTime.UtcNow;

        var users = await _db.Users
            .Include(u => u.UserFilters)
                .ThenInclude(uf => uf.Filter)
            .Where(u => u.UserFilters.Any())
            .ToListAsync();

        foreach (var user in users)
        {
            if (!IsDue(user, now)) continue;
            try { await SendDigestAsync(user); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to send digest to {Email}", user.Email); }
        }
    }

    public async Task SendDigestAsync(User user)
    {
        var from = GetFromDate(user.ReportFrequency);

        var keywords = user.UserFilters
            .Select(uf => uf.Filter.SearchKeyword.ToLower())
            .ToList();

        var events = await _db.Events
            .Include(e => e.FeedSource)
            .Where(e => e.PublishedDate >= from)
            .Where(e => keywords.Any(k =>
                e.Title.ToLower().Contains(k) ||
                e.Description.ToLower().Contains(k)))
            .OrderByDescending(e => e.PublishedDate)
            .Take(15)
            .ToListAsync();

        if (events.Count == 0)
        {
            _logger.LogInformation("No events for digest to {Email}, skipping", user.Email);
            return;
        }

        var subject = $"EventAggregator — {FrequencyLabel(user.ReportFrequency)} дайджест подій";
        var html = BuildHtml(user, events);

        await _emailService.SendAsync(user.Email, subject, html);

        user.LastReportSent = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Digest sent to {Email}: {Count} events", user.Email, events.Count);
    }

    public async Task<string> BuildPreviewAsync(User user)
    {
        var from = GetFromDate(user.ReportFrequency);

        var keywords = user.UserFilters
            .Select(uf => uf.Filter.SearchKeyword.ToLower())
            .ToList();

        var events = await _db.Events
            .Include(e => e.FeedSource)
            .Where(e => e.PublishedDate >= from)
            .Where(e => keywords.Any(k =>
                e.Title.ToLower().Contains(k) ||
                e.Description.ToLower().Contains(k)))
            .OrderByDescending(e => e.PublishedDate)
            .Take(15)
            .ToListAsync();

        return BuildHtml(user, events);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsDue(User user, DateTime now) =>
        user.ReportFrequency switch
        {
            "Daily"   => user.LastReportSent is null || (now - user.LastReportSent.Value).TotalHours  >= 23,
            "Weekly"  => user.LastReportSent is null || (now - user.LastReportSent.Value).TotalDays   >= 6.5,
            "Monthly" => user.LastReportSent is null || (now - user.LastReportSent.Value).TotalDays   >= 28,
            _         => false,
        };

    private static DateTime GetFromDate(string frequency) =>
        frequency switch
        {
            "Daily"   => DateTime.UtcNow.AddDays(-1),
            "Monthly" => DateTime.UtcNow.AddDays(-30),
            _         => DateTime.UtcNow.AddDays(-7),
        };

    private static string FrequencyLabel(string frequency) =>
        frequency switch
        {
            "Daily"   => "Щоденний",
            "Weekly"  => "Тижневий",
            "Monthly" => "Місячний",
            _         => "Персональний",
        };

    private static string BuildHtml(User user, List<Event> events)
    {
        var sb = new StringBuilder();

        sb.Append("""
            <!DOCTYPE html>
            <html lang="uk">
            <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>EventAggregator Дайджест</title>
            <style>
              body { font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 0; color: #222; }
              .wrap { max-width: 640px; margin: 0 auto; background: #fff; }
              .header { background: #1976d2; color: #fff; padding: 28px 32px; }
              .header h1 { margin: 0 0 4px; font-size: 24px; }
              .header p { margin: 0; opacity: .8; font-size: 14px; }
              .body { padding: 24px 32px; }
              .greeting { font-size: 16px; margin-bottom: 24px; }
              .event { border: 1px solid #e0e0e0; border-radius: 8px; padding: 16px; margin-bottom: 16px; }
              .event h2 { margin: 0 0 6px; font-size: 16px; }
              .event h2 a { color: #1976d2; text-decoration: none; }
              .event h2 a:hover { text-decoration: underline; }
              .meta { font-size: 12px; color: #888; margin: 0 0 8px; }
              .desc { font-size: 14px; line-height: 1.5; color: #444; margin: 0; }
              .footer { background: #f4f4f4; padding: 20px 32px; font-size: 12px; color: #999; text-align: center; }
            </style>
            </head>
            <body>
            <div class="wrap">
              <div class="header">
                <h1>EventAggregator</h1>
                <p>Персональний дайджест культурних подій</p>
              </div>
              <div class="body">
            """);

        sb.Append($"""
                <p class="greeting">Привіт, <strong>{Escape(user.FirstName)}</strong>! 👋<br/>
                Ось підбірка свіжих подій за вашими підписками:</p>
            """);

        foreach (var ev in events)
        {
            var source = ev.FeedSource?.Name ?? "Невідоме джерело";
            var date = ev.PublishedDate.ToString("dd.MM.yyyy");
            var category = string.IsNullOrEmpty(ev.Category) ? "" : $" · {Escape(ev.Category)}";
            var desc = ev.Description.Length > 200
                ? ev.Description[..200] + "…"
                : ev.Description;

            sb.Append($"""
                    <div class="event">
                      <h2><a href="{Escape(ev.SourceUrl)}">{Escape(ev.Title)}</a></h2>
                      <p class="meta">{Escape(source)} · {date}{category}</p>
                      <p class="desc">{Escape(desc)}</p>
                    </div>
                """);
        }

        sb.Append($"""
              </div>
              <div class="footer">
                <p>Ви отримали цей лист, бо підписані на розсилку EventAggregator.<br/>
                Частота розсилки: <strong>{FrequencyLabel(user.ReportFrequency)}</strong>.<br/>
                Змінити налаштування можна у <a href="http://localhost:4200/profile" style="color:#1976d2;">профілі</a>.</p>
              </div>
            </div>
            </body>
            </html>
            """);

        return sb.ToString();
    }

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
