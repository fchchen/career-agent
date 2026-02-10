using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface IResumeService
{
    Task<TailoredDocument> TailorForJobAsync(int jobId, int? masterResumeId = null);
}
