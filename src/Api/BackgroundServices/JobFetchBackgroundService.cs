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
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        // Load profile for query/location (re-read each cycle so changes take effect)
        var profile = await storageService.GetSearchProfileAsync();
        var query = !string.IsNullOrWhiteSpace(profile?.Query) ? profile.Query : SearchDefaults.DefaultQuery;
        var location = !string.IsNullOrWhiteSpace(profile?.Location) ? profile.Location : SearchDefaults.DefaultLocation;

        var queries = new List<string> { query };

        // Add a "Full Stack" variant if the primary query doesn't already contain it
        if (!query.Contains("Full Stack", StringComparison.OrdinalIgnoreCase))
        {
            queries.Add("Senior Full Stack Developer");
        }

        _logger.LogInformation("Background job fetch starting with {Count} queries (profile-driven)", queries.Count);

        foreach (var q in queries)
        {
            _logger.LogInformation("Fetching jobs for query: {Query}, location: {Location}", q, location);
            await agentService.SearchAndScoreAsync(q, location);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        _logger.LogInformation("Background job fetch complete");
    }
}
