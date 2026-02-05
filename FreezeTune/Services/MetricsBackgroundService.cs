using FreezeTune.Repositories;

namespace FreezeTune.Services;

public class MetricsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MetricsService _metrics;
    private readonly Config _config;
    private readonly ILogger<MetricsBackgroundService> _logger;

    public MetricsBackgroundService(
        IServiceProvider serviceProvider,
        MetricsService metrics,
        Config config,
        ILogger<MetricsBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _metrics = metrics;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UpdateCategoryMetrics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category metrics");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private void UpdateCategoryMetrics()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbRepo = scope.ServiceProvider.GetRequiredService<IDatabaseRepository>();

        foreach (var category in _config.Categories)
        {
            var count = dbRepo.CountForCategory(category);
            var availableUntil = dbRepo.AvailableUntil(category);
            _metrics.UpdateCategoryStats(category, count, availableUntil);
        }
    }
}