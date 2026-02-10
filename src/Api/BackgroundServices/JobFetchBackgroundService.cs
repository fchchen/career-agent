using CareerAgent.Api.Services;
using CareerAgent.Shared.Constants;

namespace CareerAgent.Api.BackgroundServices;

public class JobFetchBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobFetchBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(2);

    public JobFetchBackgroundService(IServiceProvider serviceProvider, ILogger<JobFetchBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit on startup before first fetch
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchJobsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background job fetch");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task FetchJobsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<ICareerAgentService>();

        _logger.LogInformation("Background job fetch starting");

        await agentService.SearchAndScoreAsync(
            SearchDefaults.DefaultQuery,
            SearchDefaults.DefaultLocation);

        _logger.LogInformation("Background job fetch complete");
    }
}
