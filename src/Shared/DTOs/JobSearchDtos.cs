using CareerAgent.Shared.Models;

namespace CareerAgent.Shared.DTOs;

public record JobSearchRequest(
    string? Query = null,
    string? Location = null,
    bool? RemoteOnly = null
);

public record JobListingDto(
    int Id,
    string ExternalId,
    string Source,
    string Title,
    string Company,
    string Location,
    string Description,
    string Url,
    string? Salary,
    double RelevanceScore,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    JobStatus Status,
    DateTime PostedAt,
    DateTime FetchedAt
);

public record JobStatusUpdateRequest(JobStatus Status);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
