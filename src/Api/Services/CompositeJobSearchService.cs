using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class CompositeJobSearchService : IJobSearchService
{
    private readonly IEnumerable<IJobSearchSource> _sources;
    private readonly ILogger<CompositeJobSearchService> _logger;

    public CompositeJobSearchService(IEnumerable<IJobSearchSource> sources, ILogger<CompositeJobSearchService> logger)
    {
        _sources = sources;
        _logger = logger;
    }

    public async Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false)
    {
        var tasks = _sources.Select(async source =>
        {
            try
            {
                return await source.SearchAsync(query, location, remoteOnly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job search source {Source} failed", source.GetType().Name);
                return new List<JobListing>();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }
}
