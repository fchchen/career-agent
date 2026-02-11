using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class InMemoryStorageService : IStorageService
{
    private readonly List<JobListing> _jobs = [];
    private readonly List<MasterResume> _resumes = [];
    private readonly List<TailoredDocument> _documents = [];
    private readonly List<SearchProfile> _profiles = [];
    private int _nextJobId = 1;
    private int _nextResumeId = 1;
    private int _nextDocId = 1;
    private int _nextProfileId = 1;

    public Task<JobListing?> GetJobByIdAsync(int id)
        => Task.FromResult(_jobs.FirstOrDefault(j => j.Id == id));

    public Task<JobListing?> GetJobByExternalIdAsync(string externalId, string source)
        => Task.FromResult(_jobs.FirstOrDefault(j => j.ExternalId == externalId && j.Source == source));

    public Task<List<JobListing>> GetJobsAsync(int page = 1, int pageSize = 20, JobStatus? status = null, string? sortBy = null, int? postedWithinHours = null)
    {
        var query = _jobs.AsEnumerable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        if (postedWithinHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-postedWithinHours.Value);
            query = query.Where(j => j.PostedAt >= cutoff);
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "score" => query.OrderByDescending(j => j.RelevanceScore),
            "date" => query.OrderByDescending(j => j.PostedAt),
            _ => query.OrderByDescending(j => j.RelevanceScore)
        };

        var result = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(result);
    }

    public Task<int> GetJobCountAsync(JobStatus? status = null, int? postedWithinHours = null)
    {
        var query = _jobs.AsEnumerable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        if (postedWithinHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-postedWithinHours.Value);
            query = query.Where(j => j.PostedAt >= cutoff);
        }

        return Task.FromResult(query.Count());
    }

    public Task<JobListing> UpsertJobAsync(JobListing job)
    {
        var existing = _jobs.FirstOrDefault(j => j.ExternalId == job.ExternalId && j.Source == job.Source);
        if (existing != null)
        {
            existing.Title = job.Title;
            existing.Company = job.Company;
            existing.Location = job.Location;
            existing.Description = job.Description;
            existing.Url = job.Url;
            existing.Salary = job.Salary;
            existing.RelevanceScore = job.RelevanceScore;
            existing.MatchedSkills = job.MatchedSkills;
            existing.MissingSkills = job.MissingSkills;
            existing.PostedAt = job.PostedAt;
            existing.FetchedAt = job.FetchedAt;
            return Task.FromResult(existing);
        }

        job.Id = _nextJobId++;
        _jobs.Add(job);
        return Task.FromResult(job);
    }

    public async Task UpsertManyJobsAsync(IEnumerable<JobListing> jobs)
    {
        foreach (var job in jobs)
            await UpsertJobAsync(job);
    }

    public Task UpdateJobStatusAsync(int id, JobStatus status)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == id);
        if (job != null)
            job.Status = status;
        return Task.CompletedTask;
    }

    public Task<MasterResume?> GetMasterResumeAsync(int? id = null)
    {
        var resume = id.HasValue
            ? _resumes.FirstOrDefault(r => r.Id == id.Value)
            : _resumes.FirstOrDefault();
        return Task.FromResult(resume);
    }

    public Task<MasterResume> UpsertMasterResumeAsync(MasterResume resume)
    {
        var existing = _resumes.FirstOrDefault(r => r.Id == resume.Id);
        if (existing != null)
        {
            existing.Content = resume.Content;
            existing.RawMarkdown = resume.RawMarkdown;
            existing.UpdatedAt = DateTime.UtcNow;
            return Task.FromResult(existing);
        }

        resume.Id = _nextResumeId++;
        resume.UpdatedAt = DateTime.UtcNow;
        _resumes.Add(resume);
        return Task.FromResult(resume);
    }

    public Task<TailoredDocument?> GetTailoredDocumentAsync(int id)
        => Task.FromResult(_documents.FirstOrDefault(d => d.Id == id));

    public Task<List<TailoredDocument>> GetTailoredDocumentsForJobAsync(int jobId)
        => Task.FromResult(_documents.Where(d => d.JobListingId == jobId).ToList());

    public Task<TailoredDocument> SaveTailoredDocumentAsync(TailoredDocument doc)
    {
        doc.Id = _nextDocId++;
        _documents.Add(doc);
        return Task.FromResult(doc);
    }

    public Task<SearchProfile?> GetSearchProfileAsync(int? id = null)
    {
        var profile = id.HasValue
            ? _profiles.FirstOrDefault(p => p.Id == id.Value)
            : _profiles.FirstOrDefault();
        return Task.FromResult(profile);
    }

    public Task<SearchProfile> UpsertSearchProfileAsync(SearchProfile profile)
    {
        var existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);
        if (existing != null)
        {
            existing.Name = profile.Name;
            existing.Query = profile.Query;
            existing.Location = profile.Location;
            existing.RadiusMiles = profile.RadiusMiles;
            existing.RemoteOnly = profile.RemoteOnly;
            existing.RequiredSkills = profile.RequiredSkills;
            existing.PreferredSkills = profile.PreferredSkills;
            return Task.FromResult(existing);
        }

        profile.Id = _nextProfileId++;
        _profiles.Add(profile);
        return Task.FromResult(profile);
    }

    public Task<double> GetAverageScoreAsync()
    {
        var avg = _jobs.Count > 0 ? _jobs.Average(j => j.RelevanceScore) : 0;
        return Task.FromResult(avg);
    }
}
