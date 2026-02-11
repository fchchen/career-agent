using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class CareerAgentService : ICareerAgentService
{
    private readonly IJobSearchService _searchService;
    private readonly IJobScoringService _scoringService;
    private readonly IStorageService _storageService;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<CareerAgentService> _logger;

    public CareerAgentService(
        IJobSearchService searchService,
        IJobScoringService scoringService,
        IStorageService storageService,
        IGeocodingService geocodingService,
        ILogger<CareerAgentService> logger)
    {
        _searchService = searchService;
        _scoringService = scoringService;
        _storageService = storageService;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<List<JobListing>> SearchAndScoreAsync(string? query = null, string? location = null, bool? remoteOnly = null)
    {
        var searchQuery = query ?? SearchDefaults.DefaultQuery;
        var searchLocation = location ?? SearchDefaults.DefaultLocation;
        var remote = remoteOnly ?? false;

        var profile = await _storageService.GetSearchProfileAsync();

        _logger.LogInformation("Starting search: {Query} in {Location} (remote: {Remote})", searchQuery, searchLocation, remote);

        var jobs = await _searchService.SearchAsync(searchQuery, searchLocation, remote);
        _logger.LogInformation("Found {Count} raw jobs", jobs.Count);

        foreach (var job in jobs)
        {
            var scoreResult = _scoringService.ScoreJob(job, profile);
            job.RelevanceScore = scoreResult.Score;
            job.MatchedSkills = scoreResult.MatchedSkills;
            job.MissingSkills = scoreResult.MissingSkills;
        }

        // Geocode non-remote jobs
        foreach (var job in jobs.Where(j => !j.IsRemote && !string.IsNullOrWhiteSpace(j.Location)))
        {
            var geo = await _geocodingService.GeocodeAsync(job.Location);
            if (geo is not null)
            {
                job.Latitude = geo.Latitude;
                job.Longitude = geo.Longitude;
            }
        }

        // Sort by score descending
        jobs = jobs.OrderByDescending(j => j.RelevanceScore).ToList();

        // Persist (upsert handles dedup)
        await _storageService.UpsertManyJobsAsync(jobs);

        _logger.LogInformation("Scored and saved {Count} jobs. Top score: {TopScore:F2}",
            jobs.Count, jobs.FirstOrDefault()?.RelevanceScore ?? 0);

        return jobs;
    }
}
