using CareerAgent.Api.Services;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard");

        group.MapGet("/", GetDashboard)
            .Produces<DashboardResponse>();

        return app;
    }

    private static async Task<IResult> GetDashboard(IStorageService storageService)
    {
        const int recentHours = 72;
        var totalJobs = await storageService.GetJobCountAsync(postedWithinHours: recentHours);
        var newJobs = await storageService.GetJobCountAsync(JobStatus.New, postedWithinHours: recentHours);
        var appliedJobs = await storageService.GetJobCountAsync(JobStatus.Applied, postedWithinHours: recentHours);
        var dismissedJobs = await storageService.GetJobCountAsync(JobStatus.Dismissed, postedWithinHours: recentHours);
        var avgScore = await storageService.GetAverageScoreAsync();

        var topJobs = await storageService.GetJobsAsync(1, 5, null, "score", postedWithinHours: 72);
        var recentJobs = await storageService.GetJobsAsync(1, 5, null, "date", postedWithinHours: 72);

        var stats = new DashboardStats(totalJobs, newJobs, appliedJobs, dismissedJobs, Math.Round(avgScore, 4));

        return Results.Ok(new DashboardResponse(
            stats,
            topJobs.Select(MapToDto).ToList(),
            recentJobs.Select(MapToDto).ToList()
        ));
    }

    private static JobListingDto MapToDto(JobListing j) => new(
        j.Id, j.ExternalId, j.Source, j.Title, j.Company, j.Location,
        j.Description, j.Url,
        j.ApplyLinks.Select(a => new ApplyLinkDto(a.Title, a.Url)).ToList(),
        j.Salary, j.RelevanceScore,
        j.MatchedSkills, j.MissingSkills, j.Status, j.IsRemote, j.Latitude, j.Longitude,
        j.PostedAt, j.FetchedAt);
}
