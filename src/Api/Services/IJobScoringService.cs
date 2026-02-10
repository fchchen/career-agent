using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public interface IJobScoringService
{
    JobScoreResult ScoreJob(JobListing job, SearchProfile? profile = null);
}

public record JobScoreResult(
    double Score,
    List<string> MatchedSkills,
    List<string> MissingSkills,
    Dictionary<string, double> ScoreBreakdown
);
