using CareerAgent.Api.Services;
using CareerAgent.Shared.Constants;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class CareerAgentServiceTests
{
    private readonly Mock<IJobSearchService> _searchMock = new();
    private readonly Mock<IJobScoringService> _scoringMock = new();
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly CareerAgentService _sut;

    public CareerAgentServiceTests()
    {
        _sut = new CareerAgentService(_searchMock.Object, _scoringMock.Object, _storageMock.Object, NullLogger<CareerAgentService>.Instance);
    }

    [Fact]
    public async Task SearchAndScoreAsync_ScoresAndSortsJobs()
    {
        var rawJobs = new List<JobListing>
        {
            new() { ExternalId = "1", Source = "Google Jobs", Title = "Junior Dev", Description = "Python" },
            new() { ExternalId = "2", Source = "Google Jobs", Title = "Senior .NET Engineer", Description = "C# .NET Angular" }
        };

        _searchMock.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync(rawJobs);

        _scoringMock.Setup(s => s.ScoreJob(It.Is<JobListing>(j => j.ExternalId == "1"), It.IsAny<SearchProfile?>()))
            .Returns(new JobScoreResult(0.2, ["Python"], [".NET", "C#", "Angular"], new()));

        _scoringMock.Setup(s => s.ScoreJob(It.Is<JobListing>(j => j.ExternalId == "2"), It.IsAny<SearchProfile?>()))
            .Returns(new JobScoreResult(0.9, [".NET", "C#", "Angular"], [], new()));

        _storageMock.Setup(s => s.GetSearchProfileAsync(null)).ReturnsAsync((SearchProfile?)null);
        _storageMock.Setup(s => s.UpsertManyJobsAsync(It.IsAny<IEnumerable<JobListing>>()))
            .Returns(Task.CompletedTask);

        var results = await _sut.SearchAndScoreAsync("test", "US");

        results.Should().HaveCount(2);
        results[0].RelevanceScore.Should().Be(0.9); // Highest first
        results[0].MatchedSkills.Should().Contain(".NET");
        results[1].RelevanceScore.Should().Be(0.2);

        _storageMock.Verify(s => s.UpsertManyJobsAsync(It.IsAny<IEnumerable<JobListing>>()), Times.Once);
    }

    [Fact]
    public async Task SearchAndScoreAsync_UsesDefaults_WhenNullParams()
    {
        _searchMock.Setup(s => s.SearchAsync(SearchDefaults.DefaultQuery, SearchDefaults.DefaultLocation, false))
            .ReturnsAsync([]);
        _storageMock.Setup(s => s.GetSearchProfileAsync(null)).ReturnsAsync((SearchProfile?)null);
        _storageMock.Setup(s => s.UpsertManyJobsAsync(It.IsAny<IEnumerable<JobListing>>()))
            .Returns(Task.CompletedTask);

        await _sut.SearchAndScoreAsync();

        _searchMock.Verify(s => s.SearchAsync(SearchDefaults.DefaultQuery, SearchDefaults.DefaultLocation, false), Times.Once);
    }

    [Fact]
    public async Task SearchAndScoreAsync_EmptyResults_ReturnsEmptyList()
    {
        _searchMock.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync([]);
        _storageMock.Setup(s => s.GetSearchProfileAsync(null)).ReturnsAsync((SearchProfile?)null);
        _storageMock.Setup(s => s.UpsertManyJobsAsync(It.IsAny<IEnumerable<JobListing>>()))
            .Returns(Task.CompletedTask);

        var results = await _sut.SearchAndScoreAsync("test", "US");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAndScoreAsync_UsesSearchProfile_WhenAvailable()
    {
        var profile = new SearchProfile { Location = "Chicago" };
        _storageMock.Setup(s => s.GetSearchProfileAsync(null)).ReturnsAsync(profile);

        _searchMock.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), false))
            .ReturnsAsync([new JobListing { ExternalId = "1", Source = "Google Jobs" }]);

        _scoringMock.Setup(s => s.ScoreJob(It.IsAny<JobListing>(), profile))
            .Returns(new JobScoreResult(0.5, [], [], new()));

        _storageMock.Setup(s => s.UpsertManyJobsAsync(It.IsAny<IEnumerable<JobListing>>()))
            .Returns(Task.CompletedTask);

        await _sut.SearchAndScoreAsync();

        _scoringMock.Verify(s => s.ScoreJob(It.IsAny<JobListing>(), profile), Times.Once);
    }
}
