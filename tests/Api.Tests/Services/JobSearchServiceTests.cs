using System.Net;
using System.Text;
using System.Text.Json;
using CareerAgent.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class JobSearchServiceTests
{
    // ==================== Mapping ====================

    [Fact]
    public void MapToJobListing_MapsAllFields()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "abc123",
            Title = "Senior Software Engineer",
            CompanyName = "Acme Corp",
            Location = "Remote",
            Description = "Build things with C# and .NET",
            ShareLink = "https://example.com/job/abc123",
            DetectedExtensions = new SerpApiExtensions
            {
                PostedAt = "3 days ago",
                Salary = "$120K-$150K",
                ScheduleType = "Full-time"
            },
            ApplyOptions =
            [
                new SerpApiApplyOption { Title = "Apply on LinkedIn", Link = "https://linkedin.com/jobs/123" }
            ]
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.ExternalId.Should().Be("abc123");
        result.Source.Should().Be("Google Jobs");
        result.Title.Should().Be("Senior Software Engineer");
        result.Company.Should().Be("Acme Corp");
        result.Location.Should().Be("Remote");
        result.Description.Should().Contain("C#");
        result.Url.Should().Be("https://example.com/job/abc123");
        result.Salary.Should().Be("$120K-$150K");
        result.PostedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-3), TimeSpan.FromHours(1));
    }

    [Fact]
    public void MapToJobListing_NullFields_HandledGracefully()
    {
        var serpJob = new SerpApiJob();

        var result = JobSearchService.MapToJobListing(serpJob);

        result.ExternalId.Should().NotBeNullOrEmpty(); // Generated GUID
        result.Source.Should().Be("Google Jobs");
        result.Title.Should().BeEmpty();
        result.Company.Should().BeEmpty();
    }

    [Fact]
    public void MapToJobListing_UsesApplyLink_WhenNoShareLink()
    {
        var serpJob = new SerpApiJob
        {
            ShareLink = null,
            ApplyOptions =
            [
                new SerpApiApplyOption { Link = "https://linkedin.com/apply/123" }
            ]
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.Url.Should().Be("https://linkedin.com/apply/123");
    }

    // ==================== Date Parsing ====================

    [Theory]
    [InlineData("3 days ago", -3)]
    [InlineData("1 day ago", -1)]
    [InlineData("2 weeks ago", -14)]
    [InlineData("1 week ago", -7)]
    [InlineData("1 month ago", -30)]
    public void ParseRelativeDate_ParsesCorrectly(string input, int expectedDaysOffset)
    {
        var result = JobSearchService.ParseRelativeDate(input);
        var expected = DateTime.UtcNow.AddDays(expectedDaysOffset);

        result.Should().BeCloseTo(expected, TimeSpan.FromHours(1));
    }

    [Theory]
    [InlineData("2 hours ago")]
    [InlineData("30 minutes ago")]
    [InlineData("just now")]
    [InlineData(null)]
    [InlineData("")]
    public void ParseRelativeDate_RecentOrNull_ReturnsNow(string? input)
    {
        var result = JobSearchService.ParseRelativeDate(input);
        result.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    // ==================== Search with mocked HttpClient ====================

    [Fact]
    public async Task SearchAsync_DeserializesResponse_ReturnsJobs()
    {
        var serpResponse = new SerpApiResponse
        {
            JobsResults =
            [
                new SerpApiJob
                {
                    JobId = "job1",
                    Title = "Senior .NET Developer",
                    CompanyName = "Tech Co",
                    Location = "Remote",
                    Description = "C# ASP.NET Core Angular",
                    ShareLink = "https://example.com/job1"
                },
                new SerpApiJob
                {
                    JobId = "job2",
                    Title = "Full Stack Engineer",
                    CompanyName = "StartupXYZ",
                    Location = "New York, NY",
                    Description = "React Node.js",
                    ShareLink = "https://example.com/job2"
                }
            ]
        };

        var json = JsonSerializer.Serialize(serpResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SerpApi:ApiKey"] = "test-key",
                ["SerpApi:BaseUrl"] = "https://serpapi.com/search"
            })
            .Build();

        var logger = NullLogger<JobSearchService>.Instance;

        var service = new JobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("Senior .NET Developer", "United States");

        results.Should().HaveCount(2);
        results[0].Title.Should().Be("Senior .NET Developer");
        results[1].Company.Should().Be("StartupXYZ");
    }

    [Fact]
    public async Task SearchAsync_NoApiKey_ReturnsEmpty()
    {
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SerpApi:ApiKey"] = ""
            })
            .Build();

        var logger = NullLogger<JobSearchService>.Instance;

        var service = new JobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("test", "test");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmpty()
    {
        var serpResponse = new SerpApiResponse { JobsResults = [] };
        var json = JsonSerializer.Serialize(serpResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SerpApi:ApiKey"] = "test-key",
                ["SerpApi:BaseUrl"] = "https://serpapi.com/search"
            })
            .Build();

        var logger = NullLogger<JobSearchService>.Instance;

        var service = new JobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("test", "test");
        results.Should().BeEmpty();
    }
}

/// <summary>
/// Reusable mock HttpMessageHandler for testing HttpClient-based services
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly string _response;
    private readonly HttpStatusCode _statusCode;

    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
    {
        _response = response;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage
        {
            StatusCode = _statusCode,
            Content = new StringContent(_response, Encoding.UTF8, "application/json")
        });
    }
}
