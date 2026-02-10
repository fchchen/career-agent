namespace CareerAgent.Shared.Models;

public class JobListing
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Salary { get; set; }
    public double RelevanceScore { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingSkills { get; set; } = [];
    public JobStatus Status { get; set; } = JobStatus.New;
    public DateTime PostedAt { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}

public enum JobStatus
{
    New,
    Viewed,
    Applied,
    Dismissed
}
