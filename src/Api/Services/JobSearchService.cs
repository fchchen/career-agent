using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class JobSearchService : IJobSearchSource
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

    private const int MaxPages = 2; // 2 pages × 10 results = 20 jobs, costs 2 API calls

    public async Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false)
    {
        var apiKey = _configuration["SerpApi:ApiKey"];
        var baseUrl = _configuration["SerpApi:BaseUrl"] ?? "https://serpapi.com/search";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("SerpAPI key not configured. Returning empty results.");
            return [];
        }

        // Always search for remote jobs nationwide — Adzuna covers local + remote separately
        var searchQuery = $"{query} remote";
        var serpLocation = "United States";
        var allJobs = new List<JobListing>();
        string? nextPageToken = null;

        for (var page = 0; page < MaxPages; page++)
        {
            var url = $"{baseUrl}?engine=google_jobs&q={Uri.EscapeDataString(searchQuery)}" +
                      $"&location={Uri.EscapeDataString(serpLocation)}" +
                      $"&api_key={apiKey}";

            if (nextPageToken != null)
                url += $"&next_page_token={Uri.EscapeDataString(nextPageToken)}";

            _logger.LogInformation("Searching SerpAPI for: {Query} in {Location} (page {Page})", searchQuery, serpLocation, page + 1);

            var response = await _httpClient.GetStringAsync(url);
            var serpResult = JsonSerializer.Deserialize<SerpApiResponse>(response, JsonOptions);

            if (serpResult?.JobsResults is null || serpResult.JobsResults.Count == 0)
            {
                _logger.LogInformation("No jobs found for query: {Query} (page {Page})", searchQuery, page + 1);
                break;
            }

            allJobs.AddRange(serpResult.JobsResults.Select(MapToJobListing));

            // Get next page token for pagination
            nextPageToken = serpResult.SerpApiPagination?.NextPageToken;
            if (string.IsNullOrEmpty(nextPageToken))
                break;
        }

        // Deduplicate by external ID across pages
        allJobs = allJobs.GroupBy(j => j.ExternalId).Select(g => g.First()).ToList();

        _logger.LogInformation("Mapped {Count} jobs from SerpAPI", allJobs.Count);
        return allJobs;
    }

    internal static JobListing MapToJobListing(SerpApiJob serpJob)
    {
        var postedAt = ParseRelativeDate(serpJob.DetectedExtensions?.PostedAt);
        var location = serpJob.Location ?? string.Empty;
        var description = serpJob.Description ?? string.Empty;

        var applyLinks = serpJob.ApplyOptions?
            .Where(o => !string.IsNullOrEmpty(o.Link))
            .Select(o => new ApplyLink { Title = o.Title ?? "Apply", Url = o.Link! })
            .ToList() ?? [];

        return new JobListing
        {
            ExternalId = serpJob.JobId ?? Guid.NewGuid().ToString(),
            Source = "Google Jobs",
            Title = serpJob.Title ?? string.Empty,
            Company = serpJob.CompanyName ?? string.Empty,
            Location = location,
            Description = description,
            Url = applyLinks.FirstOrDefault()?.Url ?? serpJob.ShareLink ?? string.Empty,
            ApplyLinks = applyLinks,
            Salary = serpJob.DetectedExtensions?.Salary,
            IsRemote = RemoteClassifier.ClassifyRemote(location, description, serpJob.Title),
            PostedAt = postedAt,
            FetchedAt = DateTime.UtcNow
        };
    }

    internal static string NormalizeLocationForSerpApi(string location)
    {
        // Strip zip codes (5-digit or 5+4 format)
        var result = Regex.Replace(location, @"\b\d{5}(-\d{4})?\b", "").Trim();

        // Expand US state abbreviations to full names
        result = Regex.Replace(result, @"\b([A-Z]{2})\b", match =>
            StateAbbreviations.TryGetValue(match.Value, out var full) ? full : match.Value);

        // Clean up extra commas/spaces
        result = Regex.Replace(result, @",\s*,", ",");
        result = Regex.Replace(result, @",\s*$", "");
        result = Regex.Replace(result, @"\s{2,}", " ").Trim();

        return result;
    }

    private static readonly Dictionary<string, string> StateAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AL"] = "Alabama", ["AK"] = "Alaska", ["AZ"] = "Arizona", ["AR"] = "Arkansas",
        ["CA"] = "California", ["CO"] = "Colorado", ["CT"] = "Connecticut", ["DE"] = "Delaware",
        ["FL"] = "Florida", ["GA"] = "Georgia", ["HI"] = "Hawaii", ["ID"] = "Idaho",
        ["IL"] = "Illinois", ["IN"] = "Indiana", ["IA"] = "Iowa", ["KS"] = "Kansas",
        ["KY"] = "Kentucky", ["LA"] = "Louisiana", ["ME"] = "Maine", ["MD"] = "Maryland",
        ["MA"] = "Massachusetts", ["MI"] = "Michigan", ["MN"] = "Minnesota", ["MS"] = "Mississippi",
        ["MO"] = "Missouri", ["MT"] = "Montana", ["NE"] = "Nebraska", ["NV"] = "Nevada",
        ["NH"] = "New Hampshire", ["NJ"] = "New Jersey", ["NM"] = "New Mexico", ["NY"] = "New York",
        ["NC"] = "North Carolina", ["ND"] = "North Dakota", ["OH"] = "Ohio", ["OK"] = "Oklahoma",
        ["OR"] = "Oregon", ["PA"] = "Pennsylvania", ["RI"] = "Rhode Island", ["SC"] = "South Carolina",
        ["SD"] = "South Dakota", ["TN"] = "Tennessee", ["TX"] = "Texas", ["UT"] = "Utah",
        ["VT"] = "Vermont", ["VA"] = "Virginia", ["WA"] = "Washington", ["WV"] = "West Virginia",
        ["WI"] = "Wisconsin", ["WY"] = "Wyoming", ["DC"] = "District of Columbia",
    };

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
    [JsonPropertyName("serpapi_pagination")]
    public SerpApiPagination? SerpApiPagination { get; set; }
}

public class SerpApiPagination
{
    [JsonPropertyName("next_page_token")]
    public string? NextPageToken { get; set; }
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
