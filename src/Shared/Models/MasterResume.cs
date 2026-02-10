namespace CareerAgent.Shared.Models;

public class MasterResume
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";
    public string Content { get; set; } = string.Empty;
    public string RawMarkdown { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
