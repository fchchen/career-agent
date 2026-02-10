using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;

namespace CareerAgent.Api.Services;

public class JobScoringService : IJobScoringService
{
    // Weight distribution for final score
    private const double TitleWeight = 0.30;
    private const double SkillWeight = 0.45;
    private const double LocationWeight = 0.10;
    private const double RecencyWeight = 0.15;

    public JobScoreResult ScoreJob(JobListing job, SearchProfile? profile = null)
    {
        var titleScore = ScoreTitle(job.Title);
        var (skillScore, matched, missing) = ScoreSkills(job.Description, job.Title);
        var locationScore = ScoreLocation(job.Location, profile);
        var recencyScore = ScoreRecency(job.PostedAt);

        var finalScore = (titleScore * TitleWeight)
                       + (skillScore * SkillWeight)
                       + (locationScore * LocationWeight)
                       + (recencyScore * RecencyWeight);

        // Apply negative keyword penalty
        if (HasNegativeKeywords(job.Title))
            finalScore *= 0.3;

        finalScore = Math.Clamp(finalScore, 0, 1);

        var breakdown = new Dictionary<string, double>
        {
            ["title"] = titleScore,
            ["skills"] = skillScore,
            ["location"] = locationScore,
            ["recency"] = recencyScore
        };

        return new JobScoreResult(
            Math.Round(finalScore, 4),
            matched,
            missing,
            breakdown
        );
    }

    internal static double ScoreTitle(string title)
    {
        var lowerTitle = title.ToLowerInvariant();

        // Check for exact/close matches with preferred titles
        foreach (var keyword in SearchDefaults.DefaultTitleKeywords)
        {
            if (lowerTitle.Contains(keyword.ToLowerInvariant()))
                return 1.0;
        }

        // Partial matches
        var score = 0.0;

        if (lowerTitle.Contains("senior")) score += 0.4;
        else if (lowerTitle.Contains("staff") || lowerTitle.Contains("principal") || lowerTitle.Contains("lead")) score += 0.35;

        if (lowerTitle.Contains("software engineer") || lowerTitle.Contains("software developer")) score += 0.4;
        else if (lowerTitle.Contains("full stack") || lowerTitle.Contains("fullstack")) score += 0.35;
        else if (lowerTitle.Contains("developer") || lowerTitle.Contains("engineer")) score += 0.2;

        if (lowerTitle.Contains(".net") || lowerTitle.Contains("dotnet")) score += 0.2;
        if (lowerTitle.Contains("angular")) score += 0.1;

        return Math.Min(score, 1.0);
    }

    internal static (double Score, List<string> Matched, List<string> Missing) ScoreSkills(string description, string title)
    {
        var combinedText = $"{title} {description}";
        var foundSkills = SkillTaxonomy.ExtractSkills(combinedText);

        var matched = new List<string>();
        var missing = new List<string>();

        double weightedScore = 0;
        double maxWeight = 0;

        // Score against all tracked skills
        foreach (var skill in SkillTaxonomy.CoreSkills.Concat(SkillTaxonomy.StrongSkills))
        {
            var weight = SkillTaxonomy.GetSkillWeight(skill);
            maxWeight += weight;

            if (foundSkills.Contains(skill))
            {
                matched.Add(skill);
                weightedScore += weight;
            }
            else
            {
                missing.Add(skill);
            }
        }

        // Bonus skills only add, never penalize
        foreach (var skill in SkillTaxonomy.BonusSkills)
        {
            if (foundSkills.Contains(skill))
            {
                matched.Add(skill);
                weightedScore += SkillTaxonomy.GetSkillWeight(skill) * 0.5; // Half weight bonus
            }
        }

        var score = maxWeight > 0 ? weightedScore / maxWeight : 0;
        return (Math.Min(score, 1.0), matched, missing);
    }

    internal static double ScoreLocation(string location, SearchProfile? profile)
    {
        var lowerLocation = location.ToLowerInvariant();

        // Check hybrid before remote, since "hybrid remote" contains both
        if (lowerLocation.Contains("hybrid"))
            return 0.7;

        if (lowerLocation.Contains("remote"))
            return 1.0;

        if (string.IsNullOrWhiteSpace(location))
            return 0.5;

        // If profile has a preferred location, check for match
        if (profile != null && !string.IsNullOrWhiteSpace(profile.Location))
        {
            var profileLocation = profile.Location.ToLowerInvariant();
            if (lowerLocation.Contains(profileLocation) || profileLocation.Contains(lowerLocation))
                return 0.8;
        }

        // Default for on-site with unknown location match
        return 0.4;
    }

    internal static double ScoreRecency(DateTime postedAt)
    {
        var daysOld = (DateTime.UtcNow - postedAt).TotalDays;

        return daysOld switch
        {
            <= 1 => 1.0,
            <= 3 => 0.9,
            <= 7 => 0.7,
            <= 14 => 0.5,
            <= 30 => 0.3,
            _ => 0.1
        };
    }

    internal static bool HasNegativeKeywords(string title)
    {
        var lowerTitle = title.ToLowerInvariant();
        return SearchDefaults.NegativeTitleKeywords.Any(k => lowerTitle.Contains(k.ToLowerInvariant()));
    }
}
