namespace CareerAgent.Shared.DTOs;

public record DashboardResponse(
    DashboardStats Stats,
    List<JobListingDto> TopJobs,
    List<JobListingDto> RecentJobs
);

public record DashboardStats(
    int TotalJobs,
    int NewJobs,
    int AppliedJobs,
    int DismissedJobs,
    double AverageScore
);
