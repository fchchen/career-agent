using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface IStorageService
{
    // Job Listings
    Task<JobListing?> GetJobByIdAsync(int id);
    Task<JobListing?> GetJobByExternalIdAsync(string externalId, string source);
    Task<List<JobListing>> GetJobsAsync(int page = 1, int pageSize = 20, JobStatus? status = null, string? sortBy = null, int? postedWithinHours = null, LocationFilter? locationFilter = null);
    Task<int> GetJobCountAsync(JobStatus? status = null, int? postedWithinHours = null, LocationFilter? locationFilter = null);
    Task<JobListing> UpsertJobAsync(JobListing job);
    Task UpsertManyJobsAsync(IEnumerable<JobListing> jobs);
    Task UpdateJobStatusAsync(int id, JobStatus status);

    // Master Resumes
    Task<MasterResume?> GetMasterResumeAsync(int? id = null);
    Task<MasterResume> UpsertMasterResumeAsync(MasterResume resume);

    // Tailored Documents
    Task<TailoredDocument?> GetTailoredDocumentAsync(int id);
    Task<List<TailoredDocument>> GetTailoredDocumentsForJobAsync(int jobId);
    Task<TailoredDocument> SaveTailoredDocumentAsync(TailoredDocument doc);

    // Search Profiles
    Task<SearchProfile?> GetSearchProfileAsync(int? id = null);
    Task<SearchProfile> UpsertSearchProfileAsync(SearchProfile profile);

    // Dashboard
    Task<double> GetAverageScoreAsync();
}
