using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class ResumeService : IResumeService
{
    private readonly IStorageService _storageService;
    private readonly ILlmService _llmService;
    private readonly IPdfService _pdfService;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(
        IStorageService storageService,
        ILlmService llmService,
        IPdfService pdfService,
        ILogger<ResumeService> logger)
    {
        _storageService = storageService;
        _llmService = llmService;
        _pdfService = pdfService;
        _logger = logger;
    }

    public async Task<TailoredDocument> TailorForJobAsync(int jobId, int? masterResumeId = null)
    {
        var job = await _storageService.GetJobByIdAsync(jobId)
            ?? throw new KeyNotFoundException($"Job listing {jobId} not found");

        var resume = await _storageService.GetMasterResumeAsync(masterResumeId)
            ?? throw new KeyNotFoundException("No master resume found. Please upload one first.");

        _logger.LogInformation("Tailoring resume for job {JobId}: {Title} at {Company}", jobId, job.Title, job.Company);

        var tailorResult = await _llmService.TailorResumeAsync(
            resume.RawMarkdown, job.Description, job.Title, job.Company);

        // Generate PDF
        string? pdfPath = null;
        try
        {
            var pdfBytes = _pdfService.GeneratePdf(tailorResult.TailoredResumeMarkdown, $"Resume - {job.Company}");
            var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
            Directory.CreateDirectory(outputDir);

            var fileName = $"resume_{job.Company.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            pdfPath = Path.Combine(outputDir, fileName);
            await File.WriteAllBytesAsync(pdfPath, pdfBytes);

            _logger.LogInformation("Generated PDF: {Path}", pdfPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate PDF, continuing without it");
        }

        var doc = new TailoredDocument
        {
            JobListingId = jobId,
            MasterResumeId = resume.Id,
            TailoredResumeMarkdown = tailorResult.TailoredResumeMarkdown,
            CoverLetterMarkdown = tailorResult.CoverLetterMarkdown,
            PdfPath = pdfPath,
            LlmPrompt = tailorResult.FullPrompt,
            LlmResponse = tailorResult.FullResponse,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _storageService.SaveTailoredDocumentAsync(doc);
        _logger.LogInformation("Saved tailored document {DocId} for job {JobId}", saved.Id, jobId);

        return saved;
    }
}
