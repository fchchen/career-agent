namespace CareerAgent.Shared.Constants;

public static class SearchDefaults
{
    public const string DefaultQuery = "Senior Software Engineer .NET Angular";
    public const string DefaultLocation = "United States";
    public const int DefaultRadiusMiles = 50;
    public const int MaxResultsPerSearch = 50;

    public static readonly List<string> DefaultTitleKeywords =
    [
        "Senior Software Engineer",
        "Senior Software Developer",
        "Senior Full Stack Developer",
        "Senior .NET Developer",
        "Senior Backend Engineer",
        "Staff Software Engineer",
        "Lead Software Engineer",
        "Principal Software Engineer"
    ];

    public static readonly List<string> NegativeTitleKeywords =
    [
        "Junior",
        "Intern",
        "Entry Level",
        "Associate",
        "Data Scientist",
        "Machine Learning",
        "DevOps",
        "SRE",
        "QA",
        "Test Engineer",
        "Security Engineer"
    ];
}
