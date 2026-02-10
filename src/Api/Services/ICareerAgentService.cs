using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface ICareerAgentService
{
    Task<List<JobListing>> SearchAndScoreAsync(string? query = null, string? location = null, bool? remoteOnly = null);
}
