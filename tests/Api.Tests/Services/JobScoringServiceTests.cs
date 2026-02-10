using CareerAgent.Api.Services;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class JobScoringServiceTests
{
    private readonly JobScoringService _sut = new();

    private static JobListing CreateJob(
        string title = "Senior Software Engineer",
        string description = "",
        string location = "Remote",
        DateTime? postedAt = null)
    {
        return new JobListing
        {
            Title = title,
            Description = description,
            Location = location,
            PostedAt = postedAt ?? DateTime.UtcNow
        };
    }

    // ==================== Title Scoring ====================

    [Theory]
    [InlineData("Senior Software Engineer", 1.0)]
    [InlineData("Senior Software Developer", 1.0)]
    [InlineData("Senior Full Stack Developer", 1.0)]
    [InlineData("Senior .NET Developer", 1.0)]
    [InlineData("Lead Software Engineer", 1.0)]
    [InlineData("Staff Software Engineer", 1.0)]
    [InlineData("Principal Software Engineer", 1.0)]
    public void ScoreTitle_ExactMatches_ReturnsMaxScore(string title, double expected)
    {
        var score = JobScoringService.ScoreTitle(title);
        score.Should().Be(expected);
    }

    [Fact]
    public void ScoreTitle_SeniorWithEngineer_GivesHighPartialScore()
    {
        var score = JobScoringService.ScoreTitle("Senior Backend Engineer");
        score.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public void ScoreTitle_DeveloperOnly_GivesLowScore()
    {
        var score = JobScoringService.ScoreTitle("Developer");
        score.Should().BeLessThan(0.4);
    }

    [Fact]
    public void ScoreTitle_DotNetInTitle_BoostedScore()
    {
        var score = JobScoringService.ScoreTitle("Senior .NET Engineer");
        score.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public void ScoreTitle_IrrelevantTitle_LowScore()
    {
        var score = JobScoringService.ScoreTitle("Marketing Manager");
        score.Should().BeLessThan(0.2);
    }

    // ==================== Skill Scoring ====================

    [Fact]
    public void ScoreSkills_AllCoreSkills_HighScore()
    {
        var description = "Looking for experience with C#, .NET Core, Angular, TypeScript, SQL Server, Azure, REST API, Entity Framework, Git, CI/CD, and Agile.";
        var (score, matched, _) = JobScoringService.ScoreSkills(description, "Software Engineer");

        score.Should().BeGreaterThan(0.7);
        matched.Should().Contain(".NET");
        matched.Should().Contain("C#");
        matched.Should().Contain("Angular");
        matched.Should().Contain("TypeScript");
        matched.Should().Contain("SQL Server");
        matched.Should().Contain("Azure");
    }

    [Fact]
    public void ScoreSkills_NoCoreSkills_LowScore()
    {
        var description = "Looking for Python and Django experience with PostgreSQL.";
        var (score, _, missing) = JobScoringService.ScoreSkills(description, "Software Engineer");

        score.Should().BeLessThan(0.3);
        missing.Should().Contain(".NET");
        missing.Should().Contain("C#");
        missing.Should().Contain("Angular");
    }

    [Fact]
    public void ScoreSkills_VariantNames_StillMatch()
    {
        var description = "Experience with ASP.NET Core, csharp, and T-SQL required. Must know Azure DevOps.";
        var (_, matched, _) = JobScoringService.ScoreSkills(description, "");

        matched.Should().Contain(".NET");
        matched.Should().Contain("C#");
        matched.Should().Contain("SQL Server");
        matched.Should().Contain("Azure DevOps");
    }

    [Fact]
    public void ScoreSkills_BonusSkills_OnlyAddNeverPenalize()
    {
        var descriptionWithBonus = "C#, .NET, Angular, TypeScript, SQL, Azure, Docker, Redis, Kubernetes";
        var descriptionWithout = "C#, .NET, Angular, TypeScript, SQL, Azure";

        var (scoreWith, _, _) = JobScoringService.ScoreSkills(descriptionWithBonus, "");
        var (scoreWithout, _, _) = JobScoringService.ScoreSkills(descriptionWithout, "");

        scoreWith.Should().BeGreaterThanOrEqualTo(scoreWithout);
    }

    [Fact]
    public void ScoreSkills_MissingSkills_ListsCoreAndStrong()
    {
        var description = "Only needs Python.";
        var (_, _, missing) = JobScoringService.ScoreSkills(description, "");

        missing.Should().Contain(".NET");
        missing.Should().Contain("C#");
        missing.Should().Contain("Angular");
        // Bonus skills should NOT appear in missing
        missing.Should().NotContain("Redis");
        missing.Should().NotContain("Kubernetes");
    }

    // ==================== Location Scoring ====================

    [Theory]
    [InlineData("Remote", 1.0)]
    [InlineData("Fully Remote", 1.0)]
    [InlineData("Remote - US", 1.0)]
    public void ScoreLocation_Remote_ReturnsMaxScore(string location, double expected)
    {
        var score = JobScoringService.ScoreLocation(location, null);
        score.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hybrid - New York, NY", 0.7)]
    [InlineData("Hybrid Remote/On-site", 0.7)]
    public void ScoreLocation_Hybrid_Returns07(string location, double expected)
    {
        var score = JobScoringService.ScoreLocation(location, null);
        score.Should().Be(expected);
    }

    [Fact]
    public void ScoreLocation_EmptyLocation_Returns05()
    {
        var score = JobScoringService.ScoreLocation("", null);
        score.Should().Be(0.5);
    }

    [Fact]
    public void ScoreLocation_MatchesProfileLocation_Returns08()
    {
        var profile = new SearchProfile { Location = "New York" };
        var score = JobScoringService.ScoreLocation("New York, NY", profile);
        score.Should().Be(0.8);
    }

    [Fact]
    public void ScoreLocation_NoMatch_Returns04()
    {
        var profile = new SearchProfile { Location = "San Francisco" };
        var score = JobScoringService.ScoreLocation("Austin, TX", profile);
        score.Should().Be(0.4);
    }

    // ==================== Recency Scoring ====================

    [Fact]
    public void ScoreRecency_PostedToday_ReturnsMaxScore()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow);
        score.Should().Be(1.0);
    }

    [Fact]
    public void ScoreRecency_PostedYesterday_Returns09()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow.AddDays(-2));
        score.Should().Be(0.9);
    }

    [Fact]
    public void ScoreRecency_Posted1WeekAgo_Returns07()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow.AddDays(-5));
        score.Should().Be(0.7);
    }

    [Fact]
    public void ScoreRecency_Posted2WeeksAgo_Returns05()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow.AddDays(-10));
        score.Should().Be(0.5);
    }

    [Fact]
    public void ScoreRecency_Posted1MonthAgo_Returns03()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow.AddDays(-20));
        score.Should().Be(0.3);
    }

    [Fact]
    public void ScoreRecency_PostedOver30DaysAgo_Returns01()
    {
        var score = JobScoringService.ScoreRecency(DateTime.UtcNow.AddDays(-60));
        score.Should().Be(0.1);
    }

    // ==================== Negative Keywords ====================

    [Theory]
    [InlineData("Junior Software Engineer", true)]
    [InlineData("Intern - Software Development", true)]
    [InlineData("Entry Level Developer", true)]
    [InlineData("QA Engineer", true)]
    [InlineData("Senior Software Engineer", false)]
    public void HasNegativeKeywords_DetectsCorrectly(string title, bool expected)
    {
        var result = JobScoringService.HasNegativeKeywords(title);
        result.Should().Be(expected);
    }

    // ==================== Full Score Integration ====================

    [Fact]
    public void ScoreJob_PerfectMatch_HighScore()
    {
        var job = CreateJob(
            title: "Senior Software Engineer",
            description: "We need C#, .NET Core, Angular, TypeScript, SQL Server, Azure, REST API, Entity Framework, Git, CI/CD, Agile expertise.",
            location: "Remote",
            postedAt: DateTime.UtcNow);

        var result = _sut.ScoreJob(job);

        result.Score.Should().BeGreaterThan(0.85);
        result.MatchedSkills.Should().NotBeEmpty();
        result.ScoreBreakdown.Should().ContainKey("title");
        result.ScoreBreakdown.Should().ContainKey("skills");
        result.ScoreBreakdown.Should().ContainKey("location");
        result.ScoreBreakdown.Should().ContainKey("recency");
    }

    [Fact]
    public void ScoreJob_PoorMatch_LowScore()
    {
        var job = CreateJob(
            title: "Marketing Manager",
            description: "Looking for SEO, SEM, Google Analytics, content marketing experience.",
            location: "On-site, San Francisco",
            postedAt: DateTime.UtcNow.AddDays(-45));

        var result = _sut.ScoreJob(job);

        result.Score.Should().BeLessThan(0.2);
    }

    [Fact]
    public void ScoreJob_NegativeKeyword_DrasticPenalty()
    {
        var job = CreateJob(
            title: "Junior Software Engineer",
            description: "C#, .NET, Angular, TypeScript, SQL Server, Azure",
            location: "Remote",
            postedAt: DateTime.UtcNow);

        var result = _sut.ScoreJob(job);

        // Even with matching skills and remote, junior penalty drops score significantly
        result.Score.Should().BeLessThan(0.4);
    }

    [Fact]
    public void ScoreJob_ScoreIsBetween0And1()
    {
        var job = CreateJob(description: "C#, .NET, Angular, TypeScript, SQL Server, Azure");
        var result = _sut.ScoreJob(job);

        result.Score.Should().BeGreaterThanOrEqualTo(0);
        result.Score.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void ScoreJob_WithSearchProfile_UsesProfileLocation()
    {
        var profile = new SearchProfile { Location = "Chicago" };

        var jobMatch = CreateJob(location: "Chicago, IL");
        var jobNoMatch = CreateJob(location: "Seattle, WA");

        var resultMatch = _sut.ScoreJob(jobMatch, profile);
        var resultNoMatch = _sut.ScoreJob(jobNoMatch, profile);

        resultMatch.Score.Should().BeGreaterThan(resultNoMatch.Score);
    }

    [Fact]
    public void ScoreJob_ReturnsMatchedAndMissingSkills()
    {
        var job = CreateJob(description: "Requires C# and .NET Core. Nice to have: Python.");

        var result = _sut.ScoreJob(job);

        result.MatchedSkills.Should().Contain("C#");
        result.MatchedSkills.Should().Contain(".NET");
        result.MissingSkills.Should().Contain("Angular");
        result.MissingSkills.Should().Contain("TypeScript");
    }
}
