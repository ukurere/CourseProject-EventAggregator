namespace EventAggregator.API.Services;

public class DigestBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DigestBackgroundService> _logger;

    public DigestBackgroundService(IServiceScopeFactory scopeFactory, ILogger<DigestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Digest background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var digestService = scope.ServiceProvider.GetRequiredService<DigestService>();
                await digestService.SendAllDueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digest background service error");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
