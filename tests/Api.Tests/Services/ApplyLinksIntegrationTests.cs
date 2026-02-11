using CareerAgent.Api.Data;
using CareerAgent.Api.Services;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

/// <summary>
/// Integration tests for ApplyLinks: verifies the full chain from SerpAPI mapping
/// through DB persistence to DTO serialization.
/// </summary>
public class ApplyLinksIntegrationTests : IDisposable
{
    private readonly CareerAgentDbContext _db;

    public ApplyLinksIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CareerAgentDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new CareerAgentDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    // === MapToJobListing captures apply links from SerpAPI ===

    [Fact]
    public void MapToJobListing_WithApplyOptions_CapturesAllLinks()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "test1",
            Title = "Test Job",
            CompanyName = "Test Co",
            Location = "Remote",
            ApplyOptions =
            [
                new SerpApiApplyOption { Title = "Apply on LinkedIn", Link = "https://linkedin.com/jobs/1" },
                new SerpApiApplyOption { Title = "Apply on Indeed", Link = "https://indeed.com/jobs/1" },
                new SerpApiApplyOption { Title = "Apply on ZipRecruiter", Link = "https://ziprecruiter.com/jobs/1" }
            ],
            ShareLink = "https://google.com/search?jobs"
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.ApplyLinks.Should().HaveCount(3);
        result.ApplyLinks[0].Title.Should().Be("Apply on LinkedIn");
        result.ApplyLinks[0].Url.Should().Be("https://linkedin.com/jobs/1");
        result.ApplyLinks[1].Title.Should().Be("Apply on Indeed");
        result.ApplyLinks[2].Title.Should().Be("Apply on ZipRecruiter");
    }

    [Fact]
    public void MapToJobListing_WithApplyOptions_PrefersApplyLinkOverShareLink()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "test2",
            ShareLink = "https://google.com/search?jobs",
            ApplyOptions =
            [
                new SerpApiApplyOption { Title = "Apply on LinkedIn", Link = "https://linkedin.com/jobs/1" }
            ]
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.Url.Should().Be("https://linkedin.com/jobs/1");
        result.Url.Should().NotContain("google.com");
    }

    [Fact]
    public void MapToJobListing_NoApplyOptions_FallsBackToShareLink()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "test3",
            ShareLink = "https://google.com/search?jobs",
            ApplyOptions = null
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.Url.Should().Be("https://google.com/search?jobs");
        result.ApplyLinks.Should().BeEmpty();
    }

    [Fact]
    public void MapToJobListing_EmptyApplyOptions_FallsBackToShareLink()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "test4",
            ShareLink = "https://google.com/search?jobs",
            ApplyOptions = []
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.Url.Should().Be("https://google.com/search?jobs");
        result.ApplyLinks.Should().BeEmpty();
    }

    [Fact]
    public void MapToJobListing_ApplyOptionsWithNullLinks_SkipsThem()
    {
        var serpJob = new SerpApiJob
        {
            JobId = "test5",
            ShareLink = "https://google.com/search?jobs",
            ApplyOptions =
            [
                new SerpApiApplyOption { Title = "Broken", Link = null },
                new SerpApiApplyOption { Title = "Good", Link = "https://indeed.com/jobs/1" },
                new SerpApiApplyOption { Title = "Empty", Link = "" }
            ]
        };

        var result = JobSearchService.MapToJobListing(serpJob);

        result.ApplyLinks.Should().HaveCount(1);
        result.ApplyLinks[0].Title.Should().Be("Good");
        result.Url.Should().Be("https://indeed.com/jobs/1");
    }

    // === DB round-trip: ApplyLinks survive save/load ===

    [Fact]
    public async Task SqliteStorage_ApplyLinks_SurviveRoundTrip()
    {
        var storage = new SqliteStorageService(_db);

        var job = new JobListing
        {
            ExternalId = "rt1",
            Source = "Google Jobs",
            Title = "Round Trip Test",
            Company = "Test Co",
            Location = "Remote",
            ApplyLinks =
            [
                new ApplyLink { Title = "Apply on LinkedIn", Url = "https://linkedin.com/jobs/1" },
                new ApplyLink { Title = "Apply on Indeed", Url = "https://indeed.com/jobs/1" }
            ]
        };

        await storage.UpsertJobAsync(job);

        // Clear EF cache to force a fresh read from DB
        _db.ChangeTracker.Clear();

        var loaded = await storage.GetJobByExternalIdAsync("rt1", "Google Jobs");

        loaded.Should().NotBeNull();
        loaded!.ApplyLinks.Should().HaveCount(2);
        loaded.ApplyLinks[0].Title.Should().Be("Apply on LinkedIn");
        loaded.ApplyLinks[0].Url.Should().Be("https://linkedin.com/jobs/1");
        loaded.ApplyLinks[1].Title.Should().Be("Apply on Indeed");
    }

    [Fact]
    public async Task SqliteStorage_EmptyApplyLinks_SurviveRoundTrip()
    {
        var storage = new SqliteStorageService(_db);

        var job = new JobListing
        {
            ExternalId = "rt2",
            Source = "Google Jobs",
            Title = "No Links Test",
            Company = "Test Co",
            ApplyLinks = []
        };

        await storage.UpsertJobAsync(job);
        _db.ChangeTracker.Clear();

        var loaded = await storage.GetJobByExternalIdAsync("rt2", "Google Jobs");

        loaded.Should().NotBeNull();
        loaded!.ApplyLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task SqliteStorage_UpsertUpdatesApplyLinks()
    {
        var storage = new SqliteStorageService(_db);

        // Insert with no links
        var job = new JobListing
        {
            ExternalId = "rt3",
            Source = "Google Jobs",
            Title = "Update Test",
            Company = "Test Co",
            ApplyLinks = []
        };
        await storage.UpsertJobAsync(job);

        // Upsert same job with links
        var updated = new JobListing
        {
            ExternalId = "rt3",
            Source = "Google Jobs",
            Title = "Update Test",
            Company = "Test Co",
            ApplyLinks =
            [
                new ApplyLink { Title = "Apply on LinkedIn", Url = "https://linkedin.com/jobs/1" }
            ]
        };
        await storage.UpsertJobAsync(updated);
        _db.ChangeTracker.Clear();

        var loaded = await storage.GetJobByExternalIdAsync("rt3", "Google Jobs");

        loaded.Should().NotBeNull();
        loaded!.ApplyLinks.Should().HaveCount(1);
        loaded.ApplyLinks[0].Title.Should().Be("Apply on LinkedIn");
    }

    // === GetJobsAsync returns jobs with ApplyLinks intact ===

    [Fact]
    public async Task SqliteStorage_GetJobsAsync_ReturnsApplyLinks()
    {
        var storage = new SqliteStorageService(_db);

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "list1",
            Source = "Google Jobs",
            Title = "Job With Links",
            Company = "Co",
            ApplyLinks =
            [
                new ApplyLink { Title = "Indeed", Url = "https://indeed.com/1" }
            ]
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "list2",
            Source = "Google Jobs",
            Title = "Job Without Links",
            Company = "Co",
            ApplyLinks = []
        });

        _db.ChangeTracker.Clear();
        var jobs = await storage.GetJobsAsync(1, 10);

        jobs.Should().HaveCount(2);
        var withLinks = jobs.First(j => j.ExternalId == "list1");
        var noLinks = jobs.First(j => j.ExternalId == "list2");

        withLinks.ApplyLinks.Should().HaveCount(1);
        noLinks.ApplyLinks.Should().BeEmpty();
    }

    // === InMemoryStorageService also handles ApplyLinks ===

    [Fact]
    public async Task InMemoryStorage_ApplyLinks_SurviveRoundTrip()
    {
        var storage = new InMemoryStorageService();

        var job = new JobListing
        {
            ExternalId = "mem1",
            Source = "Google Jobs",
            Title = "Memory Test",
            Company = "Test Co",
            ApplyLinks =
            [
                new ApplyLink { Title = "LinkedIn", Url = "https://linkedin.com/1" }
            ]
        };

        await storage.UpsertJobAsync(job);
        var loaded = await storage.GetJobByExternalIdAsync("mem1", "Google Jobs");

        loaded.Should().NotBeNull();
        loaded!.ApplyLinks.Should().HaveCount(1);
    }

    [Fact]
    public async Task InMemoryStorage_UpsertUpdatesApplyLinks()
    {
        var storage = new InMemoryStorageService();

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "mem2",
            Source = "Google Jobs",
            Title = "Test",
            Company = "Co",
            ApplyLinks = []
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "mem2",
            Source = "Google Jobs",
            Title = "Test",
            Company = "Co",
            ApplyLinks = [new ApplyLink { Title = "Indeed", Url = "https://indeed.com/1" }]
        });

        var loaded = await storage.GetJobByExternalIdAsync("mem2", "Google Jobs");
        loaded!.ApplyLinks.Should().HaveCount(1);
    }
}
