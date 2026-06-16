namespace EventAggregator.API.Services;

public interface ITelegramBotService
{
    Task SendMessageAsync(long chatId, string htmlText);
}
