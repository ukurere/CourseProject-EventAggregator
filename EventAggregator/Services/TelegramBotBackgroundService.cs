using EventAggregator.API.Models;
using EventAggregator.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EventAggregator.API.Services;

public class TelegramBotBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITelegramBotClient   _botClient;
    private readonly ILogger<TelegramBotBackgroundService> _logger;

    public TelegramBotBackgroundService(
        IServiceScopeFactory scopeFactory,
        ITelegramBotClient botClient,
        ILogger<TelegramBotBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _botClient    = botClient;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ReceiverOptions
        {
            AllowedUpdates     = [UpdateType.Message],
            DropPendingUpdates = true,
        };

        _botClient.StartReceiving(
            updateHandler:  HandleUpdateAsync,
            errorHandler:   HandleErrorAsync,
            receiverOptions: options,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Telegram bot started (long polling)");

        // Keep the service alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { });
    }

    // ── Update handler ────────────────────────────────────────────────────────

    private async Task HandleUpdateAsync(
        ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.Message is not { } msg) return;
        if (msg.Text  is not { } text)    return;

        var chatId   = msg.Chat.Id;
        var username = msg.From?.Username?.ToLower();

        if (text.StartsWith("/start"))
            await HandleStartAsync(client, chatId, username, ct);
        else if (text.StartsWith("/digest"))
            await HandleDigestAsync(client, chatId, ct);
        else
            await client.SendMessage(chatId,
                "Привіт! Я — EventAggregator бот 🎭\n\n" +
                "Доступні команди:\n" +
                "/start — підключити акаунт\n" +
                "/digest — отримати дайджест прямо зараз",
                cancellationToken: ct);
    }

    // ── /start ────────────────────────────────────────────────────────────────

    private async Task HandleStartAsync(
        ITelegramBotClient client, long chatId, string? username, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(username))
        {
            await client.SendMessage(chatId,
                "❌ У вашому Telegram-акаунті не налаштовано юзернейм.\n" +
                "Додайте @username у налаштуваннях Telegram і спробуйте ще раз.",
                cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.TelegramUsername != null &&
                 u.TelegramUsername.ToLower() == username, ct);

        if (user is null)
        {
            await client.SendMessage(chatId,
                $"❌ Акаунт з юзернеймом <b>@{username}</b> не знайдено в EventAggregator.\n\n" +
                "Переконайтесь, що ви правильно вказали юзернейм у профілі на сайті та увімкнули Telegram-розсилку.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        user.TelegramChatId = chatId;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Telegram linked: userId={UserId} → chatId={ChatId}", user.Id, chatId);

        await client.SendMessage(chatId,
            $"✅ Акаунт підключено!\n\n" +
            $"Привіт, <b>{Escape(user.FirstName)}</b>! 👋\n" +
            $"Тепер ти отримуватимеш персональні дайджести прямо в Telegram.\n\n" +
            $"Частота розсилки: <b>{FrequencyLabel(user.ReportFrequency)}</b>\n\n" +
            $"Відправ /digest щоб отримати добірку прямо зараз.",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    // ── /digest ───────────────────────────────────────────────────────────────

    private async Task HandleDigestAsync(
        ITelegramBotClient client, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var digestService = scope.ServiceProvider.GetRequiredService<DigestService>();

        var user = await db.Users
            .Include(u => u.UserFilters).ThenInclude(uf => uf.Filter)
            .FirstOrDefaultAsync(u => u.TelegramChatId == chatId, ct);

        if (user is null)
        {
            await client.SendMessage(chatId,
                "❌ Акаунт не підключено.\nНадішли /start для підключення.",
                cancellationToken: ct);
            return;
        }

        await client.SendMessage(chatId, "⏳ Формую дайджест...", cancellationToken: ct);

        try
        {
            await digestService.SendTelegramDigestAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending on-demand digest to chatId={ChatId}", chatId);
            await client.SendMessage(chatId,
                "❌ Помилка формування дайджесту. Спробуйте пізніше.",
                cancellationToken: ct);
        }
    }

    // ── Error handler ─────────────────────────────────────────────────────────

    private Task HandleErrorAsync(
        ITelegramBotClient client, Exception ex,
        HandleErrorSource source, CancellationToken ct)
    {
        _logger.LogWarning(ex, "Telegram polling error [{Source}]", source);
        return Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FrequencyLabel(string f) => f switch
    {
        "Daily"   => "Щоденний",
        "Weekly"  => "Тижневий",
        "Monthly" => "Місячний",
        _         => "Персональний",
    };

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
