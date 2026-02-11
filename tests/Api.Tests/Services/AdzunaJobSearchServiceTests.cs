using System.Net;
using System.Text.Json;
using CareerAgent.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class AdzunaJobSearchServiceTests
{
    // ==================== Mapping ====================

    [Fact]
    public void MapToJobListing_MapsAllFields()
    {
        var adzunaJob = new AdzunaJob
        {
            Id = 123456789,
            Title = "Senior .NET Developer",
            Description = "Build things with C# and .NET",
            Company = new AdzunaCompany { DisplayName = "Acme Corp" },
            Location = new AdzunaLocation { DisplayName = "New York, NY" },
            Latitude = 40.7128,
            Longitude = -74.0060,
            RedirectUrl = "https://adzuna.com/jobs/123",
            SalaryMin = 120000,
            SalaryMax = 150000,
            Created = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc)
        };

        var result = AdzunaJobSearchService.MapToJobListing(adzunaJob);

        result.ExternalId.Should().Be("123456789");
        result.Source.Should().Be("Adzuna");
        result.Title.Should().Be("Senior .NET Developer");
        result.Company.Should().Be("Acme Corp");
        result.Location.Should().Be("New York, NY");
        result.Description.Should().Contain("C#");
        result.Url.Should().Be("https://adzuna.com/jobs/123");
        result.ApplyLinks.Should().HaveCount(1);
        result.ApplyLinks[0].Title.Should().Be("Apply on Adzuna");
        result.ApplyLinks[0].Url.Should().Be("https://adzuna.com/jobs/123");
        result.Salary.Should().Be("$120,000 - $150,000");
        result.Latitude.Should().Be(40.7128);
        result.Longitude.Should().Be(-74.0060);
        result.PostedAt.Should().Be(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapToJobListing_NullFields_HandledGracefully()
    {
        var adzunaJob = new AdzunaJob();

        var result = AdzunaJobSearchService.MapToJobListing(adzunaJob);

        result.ExternalId.Should().NotBeNullOrEmpty();
        result.Source.Should().Be("Adzuna");
        result.Title.Should().BeEmpty();
        result.Company.Should().BeEmpty();
        result.Location.Should().BeEmpty();
        result.Url.Should().BeEmpty();
        result.ApplyLinks.Should().BeEmpty();
        result.Salary.Should().BeNull();
        result.Latitude.Should().BeNull();
        result.Longitude.Should().BeNull();
    }

    [Fact]
    public void MapToJobListing_RemoteLocation_SetsIsRemoteTrue()
    {
        var adzunaJob = new AdzunaJob
        {
            Location = new AdzunaLocation { DisplayName = "Remote" },
            Description = "Some job"
        };

        var result = AdzunaJobSearchService.MapToJobListing(adzunaJob);

        result.IsRemote.Should().BeTrue();
    }

    // ==================== Salary Formatting ====================

    [Fact]
    public void FormatSalary_Range_FormatsCorrectly()
    {
        AdzunaJobSearchService.FormatSalary(120000, 150000).Should().Be("$120,000 - $150,000");
    }

    [Fact]
    public void FormatSalary_MinOnly_FormatsCorrectly()
    {
        AdzunaJobSearchService.FormatSalary(100000, null).Should().Be("$100,000");
    }

    [Fact]
    public void FormatSalary_MaxOnly_FormatsCorrectly()
    {
        AdzunaJobSearchService.FormatSalary(null, 150000).Should().Be("$150,000");
    }

    [Fact]
    public void FormatSalary_BothNull_ReturnsNull()
    {
        AdzunaJobSearchService.FormatSalary(null, null).Should().BeNull();
    }

    [Fact]
    public void FormatSalary_BothZero_ReturnsNull()
    {
        AdzunaJobSearchService.FormatSalary(0, 0).Should().BeNull();
    }

    [Fact]
    public void FormatSalary_EqualValues_ReturnsSingleValue()
    {
        AdzunaJobSearchService.FormatSalary(100000, 100000).Should().Be("$100,000");
    }

    // ==================== Search with mocked HttpClient ====================

    [Fact]
    public async Task SearchAsync_DeserializesResponse_ReturnsJobs()
    {
        var adzunaResponse = new AdzunaResponse
        {
            Results =
            [
                new AdzunaJob
                {
                    Id = 1001,
                    Title = "Senior .NET Developer",
                    Company = new AdzunaCompany { DisplayName = "Tech Co" },
                    Location = new AdzunaLocation { DisplayName = "Remote" },
                    Description = "C# ASP.NET Core Angular",
                    RedirectUrl = "https://adzuna.com/jobs/1",
                    Latitude = 40.0,
                    Longitude = -74.0
                },
                new AdzunaJob
                {
                    Id = 1002,
                    Title = "Full Stack Engineer",
                    Company = new AdzunaCompany { DisplayName = "StartupXYZ" },
                    Location = new AdzunaLocation { DisplayName = "New York, NY" },
                    Description = "React Node.js",
                    RedirectUrl = "https://adzuna.com/jobs/2"
                }
            ]
        };

        var json = JsonSerializer.Serialize(adzunaResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adzuna:AppId"] = "test-id",
                ["Adzuna:AppKey"] = "test-key",
                ["Adzuna:BaseUrl"] = "https://api.adzuna.com/v1/api/jobs/us/search"
            })
            .Build();

        var logger = NullLogger<AdzunaJobSearchService>.Instance;
        var service = new AdzunaJobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("Senior .NET Developer", "United States");

        results.Should().HaveCount(2);
        results[0].Title.Should().Be("Senior .NET Developer");
        results[0].Source.Should().Be("Adzuna");
        results[1].Company.Should().Be("StartupXYZ");
    }

    [Fact]
    public async Task SearchAsync_MissingCredentials_ReturnsEmpty()
    {
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adzuna:AppId"] = "",
                ["Adzuna:AppKey"] = ""
            })
            .Build();

        var logger = NullLogger<AdzunaJobSearchService>.Instance;
        var service = new AdzunaJobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("test", "test");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmpty()
    {
        var adzunaResponse = new AdzunaResponse { Results = [] };
        var json = JsonSerializer.Serialize(adzunaResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Adzuna:AppId"] = "test-id",
                ["Adzuna:AppKey"] = "test-key",
                ["Adzuna:BaseUrl"] = "https://api.adzuna.com/v1/api/jobs/us/search"
            })
            .Build();

        var logger = NullLogger<AdzunaJobSearchService>.Instance;
        var service = new AdzunaJobSearchService(httpClient, config, logger);

        var results = await service.SearchAsync("test", "test");
        results.Should().BeEmpty();
    }
}
