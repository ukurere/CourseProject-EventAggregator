namespace EventAggregator.API.Services;

/// <summary>No-op implementation used when Telegram:BotToken is not configured.</summary>
public class NullTelegramBotService : ITelegramBotService
{
    private readonly ILogger<NullTelegramBotService> _logger;

    public NullTelegramBotService(ILogger<NullTelegramBotService> logger)
        => _logger = logger;

    public Task SendMessageAsync(long chatId, string htmlText)
    {
        _logger.LogDebug("Telegram not configured — skipping message to chatId={ChatId}", chatId);
        return Task.CompletedTask;
    }
}
