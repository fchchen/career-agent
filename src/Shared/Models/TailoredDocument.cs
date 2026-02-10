namespace CareerAgent.Shared.Models;

public class TailoredDocument
{
    public int Id { get; set; }
    public int JobListingId { get; set; }
    public JobListing? JobListing { get; set; }
    public int MasterResumeId { get; set; }
    public MasterResume? MasterResume { get; set; }
    public string TailoredResumeMarkdown { get; set; } = string.Empty;
    public string CoverLetterMarkdown { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public string LlmPrompt { get; set; } = string.Empty;
    public string LlmResponse { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
