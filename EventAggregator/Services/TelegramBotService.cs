using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EventAggregator.API.Services;

public class TelegramBotService : ITelegramBotService
{
    private readonly ITelegramBotClient _client;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(ITelegramBotClient client, ILogger<TelegramBotService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendMessageAsync(long chatId, string htmlText)
    {
        // Telegram message limit is 4096 chars — split if needed
        const int limit = 4000;

        if (htmlText.Length <= limit)
        {
            await SendChunkAsync(chatId, htmlText);
            return;
        }

        // Split on double newline boundaries
        var parts = SplitText(htmlText, limit);
        foreach (var part in parts)
            await SendChunkAsync(chatId, part);
    }

    private async Task SendChunkAsync(long chatId, string text)
    {
        try
        {
            await _client.SendMessage(chatId, text, parseMode: ParseMode.Html);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message to chatId={ChatId}", chatId);
        }
    }

    private static List<string> SplitText(string text, int limit)
    {
        var parts = new List<string>();
        var lines = text.Split('\n');
        var current = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (current.Length + line.Length + 1 > limit)
            {
                parts.Add(current.ToString().Trim());
                current.Clear();
            }
            current.AppendLine(line);
        }

        if (current.Length > 0)
            parts.Add(current.ToString().Trim());

        return parts;
    }
}
