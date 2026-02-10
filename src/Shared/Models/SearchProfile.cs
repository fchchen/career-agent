namespace CareerAgent.Shared.Models;

public class SearchProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default";
    public string Query { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int RadiusMiles { get; set; } = 50;
    public bool RemoteOnly { get; set; }
    public List<string> RequiredSkills { get; set; } = [];
    public List<string> PreferredSkills { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
