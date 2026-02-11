using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface IJobSearchSource
{
    Task<List<JobListing>> SearchAsync(string query, string location, bool remoteOnly = false);
}
