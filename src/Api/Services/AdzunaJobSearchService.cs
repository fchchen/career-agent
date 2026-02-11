using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class AdzunaJobSearchService : IJobSearchSource
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdzunaJobSearchService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public AdzunaJobSearchService(HttpClient httpClient, IConfiguration configuration, ILogger<AdzunaJobSearchService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false)
    {
        var appId = _configuration["Adzuna:AppId"];
        var appKey = _configuration["Adzuna:AppKey"];
        var baseUrl = _configuration["Adzuna:BaseUrl"] ?? "https://api.adzuna.com/v1/api/jobs/us/search";

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
        {
            _logger.LogWarning("Adzuna credentials not configured. Returning empty results.");
            return [];
        }

        var half = SearchDefaults.MaxResultsPerSearch / 2;
        var simplified = SimplifyQuery(query);
        var tasks = new List<Task<List<JobListing>>>();

        // Location-based search with simplified query (Adzuna ANDs all keywords,
        // so "Senior Software Engineer .NET Angular" returns almost nothing locally)
        var locationQuery = remoteOnly ? $"{simplified} remote" : simplified;
        tasks.Add(FetchAsync(baseUrl, appId, appKey, locationQuery, location, half));

        // Also search for remote jobs when not already remote-only
        if (!remoteOnly)
            tasks.Add(FetchAsync(baseUrl, appId, appKey, $"{simplified} remote", null, half));

        var results = await Task.WhenAll(tasks);
        var jobs = results.SelectMany(r => r).ToList();

        // Deduplicate by external ID
        jobs = jobs.GroupBy(j => j.ExternalId).Select(g => g.First()).ToList();

        _logger.LogInformation("Mapped {Count} jobs from Adzuna", jobs.Count);
        return jobs;
    }

    private async Task<List<JobListing>> FetchAsync(string baseUrl, string appId, string appKey,
        string query, string? location, int maxResults)
    {
        var url = $"{baseUrl}/1?app_id={Uri.EscapeDataString(appId)}" +
                  $"&app_key={Uri.EscapeDataString(appKey)}" +
                  $"&what={Uri.EscapeDataString(query)}" +
                  $"&results_per_page={maxResults}" +
                  $"&max_days_old=3" +
                  $"&sort_by=relevance";

        if (!string.IsNullOrEmpty(location) && !IsBroadLocation(location))
            url += $"&where={Uri.EscapeDataString(location)}";

        _logger.LogInformation("Searching Adzuna for: {Query} in {Location}", query, location ?? "(nationwide)");

        var bytes = await _httpClient.GetByteArrayAsync(url);
        var adzunaResult = JsonSerializer.Deserialize<AdzunaResponse>(bytes, JsonOptions);

        if (adzunaResult?.Results is null || adzunaResult.Results.Count == 0)
            return [];

        return adzunaResult.Results.Select(MapToJobListing).ToList();
    }

    internal static JobListing MapToJobListing(AdzunaJob adzunaJob)
    {
        var location = adzunaJob.Location?.DisplayName ?? string.Empty;
        var description = adzunaJob.Description ?? string.Empty;

        var applyLinks = new List<ApplyLink>();
        if (!string.IsNullOrEmpty(adzunaJob.RedirectUrl))
        {
            applyLinks.Add(new ApplyLink { Title = "Apply on Adzuna", Url = adzunaJob.RedirectUrl });
        }

        return new JobListing
        {
            ExternalId = adzunaJob.Id?.ToString(CultureInfo.InvariantCulture) ?? Guid.NewGuid().ToString(),
            Source = "Adzuna",
            Title = adzunaJob.Title ?? string.Empty,
            Company = adzunaJob.Company?.DisplayName ?? string.Empty,
            Location = location,
            Description = description,
            Url = adzunaJob.RedirectUrl ?? string.Empty,
            ApplyLinks = applyLinks,
            Salary = FormatSalary(adzunaJob.SalaryMin, adzunaJob.SalaryMax),
            IsRemote = RemoteClassifier.ClassifyRemote(location, description, adzunaJob.Title),
            Latitude = adzunaJob.Latitude,
            Longitude = adzunaJob.Longitude,
            PostedAt = adzunaJob.Created ?? DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };
    }

    internal static string? FormatSalary(double? min, double? max)
    {
        if (min.HasValue && max.HasValue && min.Value > 0 && max.Value > 0)
        {
            if (Math.Abs(min.Value - max.Value) < 1)
                return $"${min.Value:N0}";
            return $"${min.Value:N0} - ${max.Value:N0}";
        }

        if (min.HasValue && min.Value > 0)
            return $"${min.Value:N0}";

        if (max.HasValue && max.Value > 0)
            return $"${max.Value:N0}";

        return null;
    }

    private static readonly HashSet<string> BroadLocations = new(StringComparer.OrdinalIgnoreCase)
    {
        "united states", "us", "usa", "anywhere", "remote"
    };

    internal static bool IsBroadLocation(string location)
    {
        return string.IsNullOrWhiteSpace(location) || BroadLocations.Contains(location.Trim());
    }

    // Adzuna ANDs all keywords, so tech-stack terms like ".NET Angular" drastically
    // reduce local results. Strip them and keep just the role/title keywords.
    private static readonly HashSet<string> TechTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        ".net", "c#", "angular", "react", "node.js", "nodejs", "python", "java",
        "typescript", "javascript", "aws", "azure", "sql", "docker", "kubernetes",
        "go", "rust", "ruby", "php", "swift", "kotlin", "vue", "svelte", "next.js",
        "spring", "django", "flask", "rails", "graphql", "mongodb", "postgresql"
    };

    internal static string SimplifyQuery(string query)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var kept = words.Where(w => !TechTerms.Contains(w)).ToArray();
        var result = string.Join(' ', kept).Trim();
        return string.IsNullOrWhiteSpace(result) ? "Software Engineer" : result;
    }
}

// Adzuna response models
public class AdzunaResponse
{
    public List<AdzunaJob>? Results { get; set; }
    public int? Count { get; set; }
}

public class AdzunaJob
{
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public AdzunaCompany? Company { get; set; }
    public AdzunaLocation? Location { get; set; }
    public AdzunaCategory? Category { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [JsonPropertyName("redirect_url")]
    public string? RedirectUrl { get; set; }

    [JsonPropertyName("salary_min")]
    public double? SalaryMin { get; set; }

    [JsonPropertyName("salary_max")]
    public double? SalaryMax { get; set; }

    public DateTime? Created { get; set; }
}

public class AdzunaCompany
{
    public string? DisplayName { get; set; }
}

public class AdzunaLocation
{
    public string? DisplayName { get; set; }
    public List<string>? Area { get; set; }
}

public class AdzunaCategory
{
    public string? Label { get; set; }
    public string? Tag { get; set; }
}
