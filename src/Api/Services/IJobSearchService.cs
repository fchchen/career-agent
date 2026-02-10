using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface IJobSearchService
{
    Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false);
}
