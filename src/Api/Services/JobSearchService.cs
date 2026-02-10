using System.Text.Json;
using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class JobSearchService : IJobSearchService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JobSearchService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public JobSearchService(HttpClient httpClient, IConfiguration configuration, ILogger<JobSearchService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false)
    {
        var apiKey = _configuration["SerpApi:ApiKey"];
        var baseUrl = _configuration["SerpApi:BaseUrl"] ?? "https://serpapi.com/search";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SerpAPI key not configured. Returning empty results.");
            return [];
        }

        var searchQuery = remoteOnly ? $"{query} remote" : query;

        var url = $"{baseUrl}?engine=google_jobs&q={Uri.EscapeDataString(searchQuery)}" +
                  $"&location={Uri.EscapeDataString(location)}" +
                  $"&api_key={apiKey}" +
                  $"&num={SearchDefaults.MaxResultsPerSearch}";

        _logger.LogInformation("Searching SerpAPI for: {Query} in {Location}", searchQuery, location);

        var response = await _httpClient.GetStringAsync(url);
        var serpResult = JsonSerializer.Deserialize<SerpApiResponse>(response, JsonOptions);

        if (serpResult?.JobsResults is null || serpResult.JobsResults.Count == 0)
        {
            _logger.LogInformation("No jobs found for query: {Query}", searchQuery);
            return [];
        }

        var jobs = serpResult.JobsResults
            .Select(MapToJobListing)
            .ToList();

        _logger.LogInformation("Mapped {Count} jobs from SerpAPI", jobs.Count);
        return jobs;
    }

    internal static JobListing MapToJobListing(SerpApiJob serpJob)
    {
        var postedAt = ParseRelativeDate(serpJob.DetectedExtensions?.PostedAt);

        return new JobListing
        {
            ExternalId = serpJob.JobId ?? Guid.NewGuid().ToString(),
            Source = "Google Jobs",
            Title = serpJob.Title ?? string.Empty,
            Company = serpJob.CompanyName ?? string.Empty,
            Location = serpJob.Location ?? string.Empty,
            Description = serpJob.Description ?? string.Empty,
            Url = serpJob.ShareLink ?? serpJob.ApplyOptions?.FirstOrDefault()?.Link ?? string.Empty,
            Salary = serpJob.DetectedExtensions?.Salary,
            PostedAt = postedAt,
            FetchedAt = DateTime.UtcNow
        };
    }

    internal static DateTime ParseRelativeDate(string? relativeDate)
    {
        if (string.IsNullOrWhiteSpace(relativeDate))
            return DateTime.UtcNow;

        var lower = relativeDate.ToLowerInvariant().Trim();

        if (lower.Contains("hour") || lower.Contains("minute") || lower.Contains("just"))
            return DateTime.UtcNow;

        if (int.TryParse(new string(lower.TakeWhile(char.IsDigit).ToArray()), out var number))
        {
            if (lower.Contains("day"))
                return DateTime.UtcNow.AddDays(-number);
            if (lower.Contains("week"))
                return DateTime.UtcNow.AddDays(-number * 7);
            if (lower.Contains("month"))
                return DateTime.UtcNow.AddDays(-number * 30);
        }

        return DateTime.UtcNow;
    }
}

// SerpAPI response models
public class SerpApiResponse
{
    public List<SerpApiJob>? JobsResults { get; set; }
    public SerpApiSearchMetadata? SearchMetadata { get; set; }
}

public class SerpApiJob
{
    public string? JobId { get; set; }
    public string? Title { get; set; }
    public string? CompanyName { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ShareLink { get; set; }
    public string? Thumbnail { get; set; }
    public SerpApiExtensions? DetectedExtensions { get; set; }
    public List<SerpApiApplyOption>? ApplyOptions { get; set; }
}

public class SerpApiExtensions
{
    public string? PostedAt { get; set; }
    public string? ScheduleType { get; set; }
    public string? Salary { get; set; }
}

public class SerpApiApplyOption
{
    public string? Title { get; set; }
    public string? Link { get; set; }
}

public class SerpApiSearchMetadata
{
    public string? Id { get; set; }
    public string? Status { get; set; }
}
