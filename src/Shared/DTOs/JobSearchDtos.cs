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
    List<ApplyLinkDto> ApplyLinks,
    string? Salary,
    double RelevanceScore,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    JobStatus Status,
    bool IsRemote,
    double? Latitude,
    double? Longitude,
    DateTime PostedAt,
    DateTime FetchedAt
);

public record ApplyLinkDto(string Title, string Url);

public record JobStatusUpdateRequest(JobStatus Status);

public record LocationFilter(
    double HomeLatitude,
    double HomeLongitude,
    double RadiusMiles,
    bool IncludeRemote = true
);

public record GeocodeRequest(string Address);

public record GeocodeResponse(double Latitude, double Longitude, string DisplayName);

public record PagedResponse<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
