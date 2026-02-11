namespace CareerAgent.Shared.DTOs;

public record SearchProfileDto(
    int Id,
    string Name,
    string Query,
    string Location,
    int RadiusMiles,
    bool RemoteOnly,
    List<string> RequiredSkills,
    List<string> PreferredSkills,
    List<string> TitleKeywords,
    List<string> NegativeTitleKeywords,
    DateTime CreatedAt
);

public record SearchProfileUpdateRequest(
    string Query,
    string Location,
    int RadiusMiles,
    bool RemoteOnly,
    List<string> RequiredSkills,
    List<string> PreferredSkills,
    List<string> TitleKeywords,
    List<string> NegativeTitleKeywords
);
