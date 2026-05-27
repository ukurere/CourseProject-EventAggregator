namespace EventAggregator.API.Services;

public class RssPollBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RssPollBackgroundService> _logger;
    private readonly TimeSpan _interval;

    public RssPollBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RssPollBackgroundService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromHours(
            double.TryParse(config["RssPoll:IntervalHours"], out var h) ? h : 3);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RSS polling service started. Interval: {Hours}h", _interval.TotalHours);

        // Перше оновлення одразу після запуску
        await PollAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_interval, stoppingToken);
            await PollAsync();
        }
    }

    private async Task PollAsync()
    {
        _logger.LogInformation("RSS poll started at {Time}", DateTime.UtcNow);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var feedService = scope.ServiceProvider.GetRequiredService<RssFeedService>();
            await feedService.RefreshAllFeedsAsync();
            _logger.LogInformation("RSS poll completed at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RSS poll failed");
        }
    }
}
