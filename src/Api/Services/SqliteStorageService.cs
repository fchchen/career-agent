using CareerAgent.Api.Data;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerAgent.Api.Services;

public class SqliteStorageService : IStorageService
{
    private readonly CareerAgentDbContext _db;

    public SqliteStorageService(CareerAgentDbContext db)
    {
        _db = db;
    }

    public async Task<JobListing?> GetJobByIdAsync(int id)
        => await _db.JobListings.FindAsync(id);

    public async Task<JobListing?> GetJobByExternalIdAsync(string externalId, string source)
        => await _db.JobListings.FirstOrDefaultAsync(j => j.ExternalId == externalId && j.Source == source);

    public async Task<List<JobListing>> GetJobsAsync(int page = 1, int pageSize = 20, JobStatus? status = null, string? sortBy = null, int? postedWithinHours = null, LocationFilter? locationFilter = null)
    {
        var query = _db.JobListings.AsQueryable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        if (postedWithinHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-postedWithinHours.Value);
            query = query.Where(j => j.PostedAt >= cutoff);
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "date" => query.OrderByDescending(j => j.PostedAt),
            _ => query.OrderByDescending(j => j.RelevanceScore)
        };

        if (locationFilter is not null)
        {
            // Load all matching jobs, then apply location filter in-memory (dataset is small)
            var all = await query.ToListAsync();
            var filtered = ApplyLocationFilter(all, locationFilter);
            return filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetJobCountAsync(JobStatus? status = null, int? postedWithinHours = null, LocationFilter? locationFilter = null)
    {
        var query = _db.JobListings.AsQueryable();

        if (status.HasValue)
            query = query.Where(j => j.Status == status.Value);

        if (postedWithinHours.HasValue)
        {
            var cutoff = DateTime.UtcNow.AddHours(-postedWithinHours.Value);
            query = query.Where(j => j.PostedAt >= cutoff);
        }

        if (locationFilter is not null)
        {
            var all = await query.ToListAsync();
            return ApplyLocationFilter(all, locationFilter).Count;
        }

        return await query.CountAsync();
    }

    private static List<JobListing> ApplyLocationFilter(List<JobListing> jobs, LocationFilter filter)
    {
        return jobs.Where(j =>
        {
            if (filter.IncludeRemote && j.IsRemote)
                return true;

            if (j.Latitude.HasValue && j.Longitude.HasValue)
            {
                var distance = GeoMath.HaversineDistanceMiles(
                    filter.HomeLatitude, filter.HomeLongitude,
                    j.Latitude.Value, j.Longitude.Value);
                return distance <= filter.RadiusMiles;
            }

            return false;
        }).ToList();
    }

    public async Task<JobListing> UpsertJobAsync(JobListing job)
    {
        var existing = await _db.JobListings
            .FirstOrDefaultAsync(j => j.ExternalId == job.ExternalId && j.Source == job.Source);

        if (existing != null)
        {
            existing.Title = job.Title;
            existing.Company = job.Company;
            existing.Location = job.Location;
            existing.Description = job.Description;
            existing.Url = job.Url;
            existing.ApplyLinks = job.ApplyLinks;
            existing.Salary = job.Salary;
            existing.RelevanceScore = job.RelevanceScore;
            existing.MatchedSkills = job.MatchedSkills;
            existing.MissingSkills = job.MissingSkills;
            existing.IsRemote = job.IsRemote;
            existing.Latitude = job.Latitude;
            existing.Longitude = job.Longitude;
            existing.PostedAt = job.PostedAt;
            existing.FetchedAt = job.FetchedAt;
        }
        else
        {
            _db.JobListings.Add(job);
        }

        await _db.SaveChangesAsync();
        return existing ?? job;
    }

    public async Task UpsertManyJobsAsync(IEnumerable<JobListing> jobs)
    {
        foreach (var job in jobs)
            await UpsertJobAsync(job);
    }

    public async Task UpdateJobStatusAsync(int id, JobStatus status)
    {
        var job = await _db.JobListings.FindAsync(id);
        if (job != null)
        {
            job.Status = status;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<MasterResume?> GetMasterResumeAsync(int? id = null)
    {
        return id.HasValue
            ? await _db.MasterResumes.FindAsync(id.Value)
            : await _db.MasterResumes.FirstOrDefaultAsync();
    }

    public async Task<MasterResume> UpsertMasterResumeAsync(MasterResume resume)
    {
        var existing = await _db.MasterResumes.FindAsync(resume.Id);
        if (existing != null)
        {
            existing.Content = resume.Content;
            existing.RawMarkdown = resume.RawMarkdown;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.MasterResumes.Add(resume);
        }

        await _db.SaveChangesAsync();
        return existing ?? resume;
    }

    public async Task<TailoredDocument?> GetTailoredDocumentAsync(int id)
        => await _db.TailoredDocuments
            .Include(d => d.JobListing)
            .Include(d => d.MasterResume)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task<List<TailoredDocument>> GetTailoredDocumentsForJobAsync(int jobId)
        => await _db.TailoredDocuments
            .Include(d => d.JobListing)
            .Where(d => d.JobListingId == jobId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<TailoredDocument> SaveTailoredDocumentAsync(TailoredDocument doc)
    {
        _db.TailoredDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return doc;
    }

    public async Task<SearchProfile?> GetSearchProfileAsync(int? id = null)
    {
        return id.HasValue
            ? await _db.SearchProfiles.FindAsync(id.Value)
            : await _db.SearchProfiles.FirstOrDefaultAsync();
    }

    public async Task<SearchProfile> UpsertSearchProfileAsync(SearchProfile profile)
    {
        var existing = await _db.SearchProfiles.FindAsync(profile.Id);
        if (existing != null)
        {
            existing.Name = profile.Name;
            existing.Query = profile.Query;
            existing.Location = profile.Location;
            existing.RadiusMiles = profile.RadiusMiles;
            existing.RemoteOnly = profile.RemoteOnly;
            existing.RequiredSkills = profile.RequiredSkills;
            existing.PreferredSkills = profile.PreferredSkills;
        }
        else
        {
            _db.SearchProfiles.Add(profile);
        }

        await _db.SaveChangesAsync();
        return existing ?? profile;
    }

    public async Task<double> GetAverageScoreAsync()
    {
        return await _db.JobListings.AnyAsync()
            ? await _db.JobListings.AverageAsync(j => j.RelevanceScore)
            : 0;
    }
}
