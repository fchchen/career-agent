using CareerAgent.Api.Data;
using CareerAgent.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CareerAgent.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("Admin");

        group.MapPost("/backfill-locations", BackfillLocations)
            .Produces<BackfillResult>();

        group.MapPost("/dedup-jobs", DedupJobs)
            .Produces<DedupResult>();

        return app;
    }

    private static async Task<IResult> DedupJobs(
        CareerAgentDbContext db,
        ILogger<Program> logger)
    {
        var allJobs = await db.JobListings.ToListAsync();

        var groups = allJobs
            .GroupBy(j => (j.Title, j.Company))
            .Where(g => g.Count() > 1)
            .ToList();

        var removed = 0;
        foreach (var group in groups)
        {
            // Keep the one with the most apply links; ties broken by lowest ID
            var sorted = group.OrderByDescending(j => j.ApplyLinks.Count).ThenBy(j => j.Id).ToList();
            var keep = sorted.First();
            foreach (var dupe in sorted.Skip(1))
            {
                db.JobListings.Remove(dupe);
                removed++;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Dedup complete: removed {Removed} duplicate jobs from {Groups} groups", removed, groups.Count);

        return Results.Ok(new DedupResult(removed, groups.Count));
    }

    private record DedupResult(int Removed, int DuplicateGroups);

    private static async Task<IResult> BackfillLocations(
        IStorageService storageService,
        IGeocodingService geocodingService,
        ILogger<Program> logger)
    {
        var page = 1;
        const int pageSize = 100;
        var classified = 0;
        var geocoded = 0;
        var total = 0;

        while (true)
        {
            var jobs = await storageService.GetJobsAsync(page, pageSize);
            if (jobs.Count == 0) break;

            foreach (var job in jobs)
            {
                total++;
                var wasRemote = job.IsRemote;
                job.IsRemote = JobSearchService.ClassifyRemote(job.Location, job.Description);

                if (job.IsRemote != wasRemote)
                    classified++;

                if (!job.IsRemote && !job.Latitude.HasValue && !string.IsNullOrWhiteSpace(job.Location))
                {
                    var geo = await geocodingService.GeocodeAsync(job.Location);
                    if (geo is not null)
                    {
                        job.Latitude = geo.Latitude;
                        job.Longitude = geo.Longitude;
                        geocoded++;
                    }
                }

                await storageService.UpsertJobAsync(job);
            }

            if (jobs.Count < pageSize) break;
            page++;
        }

        logger.LogInformation("Backfill complete: {Total} jobs processed, {Classified} reclassified, {Geocoded} geocoded",
            total, classified, geocoded);

        return Results.Ok(new BackfillResult(total, classified, geocoded));
    }

    private record BackfillResult(int TotalProcessed, int Reclassified, int Geocoded);
}
