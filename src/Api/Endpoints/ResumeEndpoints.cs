using CareerAgent.Api.Services;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareerAgent.Api.Endpoints;

public static class ResumeEndpoints
{
    public static IEndpointRouteBuilder MapResumeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/resume")
            .WithTags("Resume");

        group.MapGet("/", GetMasterResume)
            .Produces<MasterResumeDto>()
            .Produces(404);

        group.MapPut("/", UpdateMasterResume)
            .Produces<MasterResumeDto>();

        group.MapPost("/tailor/{jobId:int}", TailorResume)
            .Produces<TailoredDocumentDto>()
            .Produces(404);

        group.MapGet("/tailored/{id:int}", GetTailoredDocument)
            .Produces<TailoredDocumentDto>()
            .Produces(404);

        group.MapGet("/tailored/{id:int}/pdf", DownloadPdf)
            .Produces(200)
            .Produces(404);

        group.MapGet("/tailored/job/{jobId:int}", GetTailoredDocumentsForJob)
            .Produces<List<TailoredDocumentDto>>();

        return app;
    }

    private static async Task<IResult> GetMasterResume(IStorageService storageService)
    {
        var resume = await storageService.GetMasterResumeAsync();
        if (resume is null) return Results.NotFound();
        return Results.Ok(MapResumeToDto(resume));
    }

    private static async Task<IResult> UpdateMasterResume(
        [FromBody] MasterResumeUpdateRequest request,
        IStorageService storageService)
    {
        var existing = await storageService.GetMasterResumeAsync();
        var resume = existing ?? new MasterResume();

        resume.Content = request.Content;
        resume.RawMarkdown = request.RawMarkdown;

        var saved = await storageService.UpsertMasterResumeAsync(resume);
        return Results.Ok(MapResumeToDto(saved));
    }

    private static async Task<IResult> TailorResume(
        int jobId,
        [FromBody] TailorRequest? request,
        IResumeService resumeService)
    {
        try
        {
            var doc = await resumeService.TailorForJobAsync(jobId, request?.MasterResumeId);
            return Results.Ok(MapTailoredToDto(doc));
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetTailoredDocument(
        int id,
        IStorageService storageService)
    {
        var doc = await storageService.GetTailoredDocumentAsync(id);
        if (doc is null) return Results.NotFound();
        return Results.Ok(MapTailoredToDto(doc));
    }

    private static async Task<IResult> DownloadPdf(
        int id,
        IStorageService storageService,
        IPdfService pdfService)
    {
        var doc = await storageService.GetTailoredDocumentAsync(id);
        if (doc is null) return Results.NotFound();

        // If we have a saved PDF file, serve it
        if (!string.IsNullOrEmpty(doc.PdfPath) && File.Exists(doc.PdfPath))
        {
            var bytes = await File.ReadAllBytesAsync(doc.PdfPath);
            return Results.File(bytes, "application/pdf", Path.GetFileName(doc.PdfPath));
        }

        // Otherwise generate on-the-fly
        var pdfBytes = pdfService.GeneratePdf(doc.TailoredResumeMarkdown, "Tailored Resume");
        var fileName = $"resume_{doc.Id}.pdf";
        return Results.File(pdfBytes, "application/pdf", fileName);
    }

    private static async Task<IResult> GetTailoredDocumentsForJob(
        int jobId,
        IStorageService storageService)
    {
        var docs = await storageService.GetTailoredDocumentsForJobAsync(jobId);
        return Results.Ok(docs.Select(MapTailoredToDto).ToList());
    }

    private static MasterResumeDto MapResumeToDto(MasterResume r) => new(
        r.Id, r.Name, r.Content, r.RawMarkdown, r.UpdatedAt);

    private static TailoredDocumentDto MapTailoredToDto(TailoredDocument d) => new(
        d.Id, d.JobListingId,
        d.JobListing?.Title ?? string.Empty,
        d.JobListing?.Company ?? string.Empty,
        d.TailoredResumeMarkdown, d.CoverLetterMarkdown,
        d.PdfPath, d.CreatedAt);
}
