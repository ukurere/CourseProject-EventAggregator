using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EventAggregator.API.Services;

public class DigestService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ITelegramBotService _telegramService;
    private readonly ILogger<DigestService> _logger;

    public DigestService(AppDbContext db, IEmailService emailService,
                         ITelegramBotService telegramService, ILogger<DigestService> logger)
    {
        _db               = db;
        _emailService     = emailService;
        _telegramService  = telegramService;
        _logger           = logger;
    }

    /// <summary>
    /// Відправка на вимогу (кнопка «Надіслати зараз»).
    /// Завжди бере останні 30 днів, повертає кількість надісланих подій.
    /// Кидає InvalidOperationException якщо нічого не знайдено.
    /// </summary>
    public async Task<int> SendNowAsync(User user, bool telegramOnly = false)
    {
        var from = DateTime.UtcNow.AddDays(-30);

        var keywords = user.UserFilters
            .Select(uf => uf.Filter.SearchKeyword.ToLower())
            .ToList();

        var personalKeywords = await _db.UserKeywords
            .Where(k => k.UserId == user.Id)
            .Select(k => k.Keyword)
            .ToListAsync();
        keywords.AddRange(personalKeywords);

        var prefLang = string.IsNullOrWhiteSpace(user.PreferredLanguage) ? null : user.PreferredLanguage;

        var events = await _db.Events
            .Include(e => e.FeedSource)
            .Where(e => e.PublishedDate >= from)
            .Where(e => !keywords.Any() || keywords.Any(k =>
                e.Title.ToLower().Contains(k) ||
                e.Description.ToLower().Contains(k)))
            .Where(e => prefLang == null || e.FeedSource!.Language == prefLang)
            .OrderByDescending(e => e.PublishedDate)
            .Take(15)
            .ToListAsync();

        if (events.Count == 0)
            throw new InvalidOperationException(
                "Не знайдено жодної події за останні 30 днів за вашими підписками. " +
                "Спробуйте додати більше ключових слів або категорій.");

        if (!telegramOnly)
        {
            var subject = "EventAggregator — Дайджест на ваш запит";
            var html = BuildHtml(user, events);
            await _emailService.SendAsync(user.Email, subject, html);
        }

        if (user.TelegramNotificationsEnabled && user.TelegramChatId.HasValue)
        {
            var tgText = BuildTelegramText(user, events);
            await _telegramService.SendMessageAsync(user.TelegramChatId.Value, tgText);
        }

        return events.Count;
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

        // Додаємо персональні ключові слова користувача
        var personalKeywords = await _db.UserKeywords
            .Where(k => k.UserId == user.Id)
            .Select(k => k.Keyword)
            .ToListAsync();
        keywords.AddRange(personalKeywords);

        var events = await _db.Events
            .Include(e => e.FeedSource)
            .Where(e => e.PublishedDate >= from)
            .Where(e => !keywords.Any() || keywords.Any(k =>
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

        // Telegram
        if (user.TelegramNotificationsEnabled && user.TelegramChatId.HasValue)
        {
            var tgText = BuildTelegramText(user, events);
            await _telegramService.SendMessageAsync(user.TelegramChatId.Value, tgText);
        }

        user.LastReportSent = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Digest sent to {Email}: {Count} events", user.Email, events.Count);
    }

    /// <summary>Send digest only to Telegram (called by /digest bot command).</summary>
    public async Task SendTelegramDigestAsync(User user)
    {
        if (!user.TelegramChatId.HasValue) return;

        var from     = GetFromDate(user.ReportFrequency);
        var keywords = user.UserFilters
            .Select(uf => uf.Filter.SearchKeyword.ToLower())
            .ToList();

        var personalKeywords = await _db.UserKeywords
            .Where(k => k.UserId == user.Id)
            .Select(k => k.Keyword)
            .ToListAsync();
        keywords.AddRange(personalKeywords);

        var events = await _db.Events
            .Include(e => e.FeedSource)
            .Where(e => e.PublishedDate >= from)
            .Where(e => !keywords.Any() || keywords.Any(k =>
                e.Title.ToLower().Contains(k) ||
                e.Description.ToLower().Contains(k)))
            .OrderByDescending(e => e.PublishedDate)
            .Take(10)
            .ToListAsync();

        if (events.Count == 0)
        {
            await _telegramService.SendMessageAsync(user.TelegramChatId.Value,
                "ℹ️ За вашими підписками поки що немає нових подій.");
            return;
        }

        var tgText = BuildTelegramText(user, events);
        await _telegramService.SendMessageAsync(user.TelegramChatId.Value, tgText);
        _logger.LogInformation("Telegram digest sent to chatId={ChatId}: {Count} events",
            user.TelegramChatId.Value, events.Count);
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

    private static string BuildTelegramText(User user, List<Event> events)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"📅 <b>EventAggregator — {FrequencyLabel(user.ReportFrequency)} дайджест</b>");
        sb.AppendLine();
        sb.AppendLine($"Привіт, <b>{TgEscape(user.FirstName)}</b>! 👋");
        sb.AppendLine("Ось підбірка свіжих подій за вашими підписками:");
        sb.AppendLine();

        for (int i = 0; i < events.Count; i++)
        {
            var ev     = events[i];
            var source = TgEscape(ev.FeedSource?.Name ?? "Невідоме джерело");
            var date   = ev.PublishedDate.ToString("dd.MM.yyyy");
            var cat    = string.IsNullOrEmpty(ev.Category) ? "" : $" · {TgEscape(ev.Category)}";
            var desc   = ev.Description.Length > 150
                ? ev.Description[..150] + "…"
                : ev.Description;

            sb.AppendLine($"<b>{i + 1}. {TgEscape(ev.Title)}</b>");
            sb.AppendLine($"📰 <i>{source} · {date}{cat}</i>");
            sb.AppendLine(TgEscape(desc));
            sb.AppendLine($"🔗 <a href=\"{TgEscape(ev.SourceUrl)}\">Читати далі</a>");
            sb.AppendLine();
        }

        sb.AppendLine($"<i>Частота розсилки: {FrequencyLabel(user.ReportFrequency)}</i>");

        return sb.ToString().Trim();
    }

    /// <summary>Escapes characters that have special meaning in Telegram HTML mode.</summary>
    private static string TgEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
