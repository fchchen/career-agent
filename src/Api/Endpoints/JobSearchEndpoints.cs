using CareerAgent.Api.Services;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareerAgent.Api.Endpoints;

public static class JobSearchEndpoints
{
    public static IEndpointRouteBuilder MapJobSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/jobs")
            .WithTags("Jobs");

        group.MapGet("/", GetJobs)
            .Produces<PagedResponse<JobListingDto>>();

        group.MapPost("/search", SearchJobs)
            .Produces<List<JobListingDto>>();

        group.MapGet("/{id:int}", GetJobById)
            .Produces<JobListingDto>()
            .Produces(404);

        group.MapPatch("/{id:int}/status", UpdateJobStatus)
            .Produces(204)
            .Produces(404);

        group.MapPost("/geocode", GeocodeAddress)
            .Produces<GeocodeResponse>()
            .Produces(404);

        return app;
    }

    private static async Task<IResult> GetJobs(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] int? postedWithinHours,
        [FromQuery] double? homeLatitude,
        [FromQuery] double? homeLongitude,
        [FromQuery] double? radiusMiles,
        [FromQuery] bool? includeRemote,
        IStorageService storageService)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;

        JobStatus? statusFilter = null;
        if (Enum.TryParse<JobStatus>(status, true, out var parsed))
            statusFilter = parsed;

        LocationFilter? locationFilter = null;
        if (homeLatitude.HasValue && homeLongitude.HasValue && radiusMiles.HasValue)
            locationFilter = new LocationFilter(homeLatitude.Value, homeLongitude.Value, radiusMiles.Value, includeRemote ?? true);

        var jobs = await storageService.GetJobsAsync(page, pageSize, statusFilter, sortBy, postedWithinHours, locationFilter);
        var totalCount = await storageService.GetJobCountAsync(statusFilter, postedWithinHours, locationFilter);

        var dtos = jobs.Select(MapToDto).ToList();
        return Results.Ok(new PagedResponse<JobListingDto>(dtos, totalCount, page, pageSize));
    }

    private static async Task<IResult> SearchJobs(
        [FromBody] JobSearchRequest request,
        ICareerAgentService agentService)
    {
        var jobs = await agentService.SearchAndScoreAsync(request.Query, request.Location, request.RemoteOnly);
        var dtos = jobs.Select(MapToDto).ToList();
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetJobById(
        int id,
        IStorageService storageService)
    {
        var job = await storageService.GetJobByIdAsync(id);
        if (job is null) return Results.NotFound();

        // Mark as viewed
        if (job.Status == JobStatus.New)
            await storageService.UpdateJobStatusAsync(id, JobStatus.Viewed);

        return Results.Ok(MapToDto(job));
    }

    private static async Task<IResult> UpdateJobStatus(
        int id,
        [FromBody] JobStatusUpdateRequest request,
        IStorageService storageService)
    {
        var job = await storageService.GetJobByIdAsync(id);
        if (job is null) return Results.NotFound();

        await storageService.UpdateJobStatusAsync(id, request.Status);
        return Results.NoContent();
    }

    private static async Task<IResult> GeocodeAddress(
        [FromBody] GeocodeRequest request,
        IGeocodingService geocodingService)
    {
        var result = await geocodingService.GeocodeAsync(request.Address);
        if (result is null)
            return Results.NotFound(new { message = $"Could not geocode '{request.Address}'" });

        return Results.Ok(new GeocodeResponse(result.Latitude, result.Longitude, result.DisplayName));
    }

    private static JobListingDto MapToDto(JobListing j) => new(
        j.Id, j.ExternalId, j.Source, j.Title, j.Company, j.Location,
        j.Description, j.Url,
        j.ApplyLinks.Select(a => new ApplyLinkDto(a.Title, a.Url)).ToList(),
        j.Salary, j.RelevanceScore,
        j.MatchedSkills, j.MissingSkills, j.Status, j.IsRemote, j.Latitude, j.Longitude,
        j.PostedAt, j.FetchedAt);
}
