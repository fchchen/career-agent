using CareerAgent.Api.Data;
using CareerAgent.Api.Services;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

/// <summary>
/// Tests for the location filter: IsRemote classification, geocoding fields,
/// haversine filtering in both storage implementations.
/// </summary>
public class LocationFilterTests : IDisposable
{
    private readonly CareerAgentDbContext _db;

    // Rochester Hills, MI coordinates
    private const double HomeLat = 42.6583;
    private const double HomeLon = -83.1499;

    public LocationFilterTests()
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

    // === IsRemote + Lat/Lon fields survive DB round-trip ===

    [Fact]
    public async Task SqliteStorage_LocationFields_SurviveRoundTrip()
    {
        var storage = new SqliteStorageService(_db);

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "loc1", Source = "Google Jobs", Title = "Remote Job",
            IsRemote = true, Latitude = null, Longitude = null
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "loc2", Source = "Google Jobs", Title = "Detroit Job",
            IsRemote = false, Latitude = 42.3314, Longitude = -83.0458
        });

        _db.ChangeTracker.Clear();

        var remote = await storage.GetJobByExternalIdAsync("loc1", "Google Jobs");
        remote!.IsRemote.Should().BeTrue();
        remote.Latitude.Should().BeNull();

        var detroit = await storage.GetJobByExternalIdAsync("loc2", "Google Jobs");
        detroit!.IsRemote.Should().BeFalse();
        detroit.Latitude.Should().BeApproximately(42.3314, 0.001);
        detroit.Longitude.Should().BeApproximately(-83.0458, 0.001);
    }

    [Fact]
    public async Task SqliteStorage_UpsertUpdatesLocationFields()
    {
        var storage = new SqliteStorageService(_db);

        // Insert with no location data
        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "loc3", Source = "Google Jobs", Title = "Job",
            IsRemote = false, Latitude = null, Longitude = null
        });

        // Upsert with location data
        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "loc3", Source = "Google Jobs", Title = "Job",
            IsRemote = true, Latitude = 40.7128, Longitude = -74.0060
        });

        _db.ChangeTracker.Clear();
        var loaded = await storage.GetJobByExternalIdAsync("loc3", "Google Jobs");
        loaded!.IsRemote.Should().BeTrue();
        loaded.Latitude.Should().BeApproximately(40.7128, 0.001);
    }

    // === LocationFilter in SqliteStorageService ===

    [Fact]
    public async Task SqliteStorage_LocationFilter_IncludesRemoteJobs()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().Contain(j => j.ExternalId == "remote1");
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_ExcludesRemoteWhenDisabled()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: false);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().NotContain(j => j.ExternalId == "remote1");
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_IncludesNearbyJobs()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        // Detroit is ~23 miles from Rochester Hills
        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: false);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().Contain(j => j.ExternalId == "detroit1");
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_ExcludesFarJobs()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        // Chicago is ~235 miles from Rochester Hills
        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: false);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().NotContain(j => j.ExternalId == "chicago1");
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_ExcludesNonGeocodedNonRemoteJobs()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().NotContain(j => j.ExternalId == "unknown1");
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_CountMatchesResults()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);
        var count = await storage.GetJobCountAsync(locationFilter: filter);

        count.Should().Be(jobs.Count);
    }

    [Fact]
    public async Task SqliteStorage_LocationFilter_PaginatesCorrectly()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);

        var allJobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);
        var page1 = await storage.GetJobsAsync(1, 1, locationFilter: filter);
        var page2 = await storage.GetJobsAsync(2, 1, locationFilter: filter);

        page1.Should().HaveCount(1);
        if (allJobs.Count > 1)
        {
            page2.Should().HaveCount(1);
            page1[0].Id.Should().NotBe(page2[0].Id);
        }
    }

    [Fact]
    public async Task SqliteStorage_NoLocationFilter_ReturnsAllJobs()
    {
        var storage = new SqliteStorageService(_db);
        await SeedLocationTestData(storage);

        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: null);

        jobs.Should().HaveCount(4);
    }

    // === Same filter logic in InMemoryStorageService ===

    [Fact]
    public async Task InMemoryStorage_LocationFilter_IncludesRemoteAndNearby()
    {
        var storage = new InMemoryStorageService();
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);

        jobs.Should().Contain(j => j.ExternalId == "remote1");
        jobs.Should().Contain(j => j.ExternalId == "detroit1");
        jobs.Should().NotContain(j => j.ExternalId == "chicago1");
        jobs.Should().NotContain(j => j.ExternalId == "unknown1");
    }

    [Fact]
    public async Task InMemoryStorage_LocationFilter_CountMatchesResults()
    {
        var storage = new InMemoryStorageService();
        await SeedLocationTestData(storage);

        var filter = new LocationFilter(HomeLat, HomeLon, RadiusMiles: 30, IncludeRemote: true);
        var jobs = await storage.GetJobsAsync(1, 100, locationFilter: filter);
        var count = await storage.GetJobCountAsync(locationFilter: filter);

        count.Should().Be(jobs.Count);
    }

    // === Helper ===

    private static async Task SeedLocationTestData(IStorageService storage)
    {
        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "remote1", Source = "Google Jobs", Title = "Remote Job",
            Company = "A", Location = "Remote",
            IsRemote = true, Latitude = null, Longitude = null
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "detroit1", Source = "Google Jobs", Title = "Detroit Job",
            Company = "B", Location = "Detroit, MI",
            IsRemote = false, Latitude = 42.3314, Longitude = -83.0458  // ~23mi from Rochester Hills
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "chicago1", Source = "Google Jobs", Title = "Chicago Job",
            Company = "C", Location = "Chicago, IL",
            IsRemote = false, Latitude = 41.8781, Longitude = -87.6298  // ~235mi from Rochester Hills
        });

        await storage.UpsertJobAsync(new JobListing
        {
            ExternalId = "unknown1", Source = "Google Jobs", Title = "Unknown Location Job",
            Company = "D", Location = "Somewhere",
            IsRemote = false, Latitude = null, Longitude = null  // Not geocoded
        });
    }
}
