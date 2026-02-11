using CareerAgent.Api.Services;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class CompositeJobSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_MergesResultsFromMultipleSources()
    {
        var source1 = new Mock<IJobSearchSource>();
        source1.Setup(s => s.SearchAsync("test", "US", false))
            .ReturnsAsync([new JobListing { ExternalId = "1", Source = "Google Jobs" }]);

        var source2 = new Mock<IJobSearchSource>();
        source2.Setup(s => s.SearchAsync("test", "US", false))
            .ReturnsAsync([new JobListing { ExternalId = "2", Source = "Adzuna" }]);

        var sut = new CompositeJobSearchService(
            [source1.Object, source2.Object],
            NullLogger<CompositeJobSearchService>.Instance);

        var results = await sut.SearchAsync("test", "US");

        results.Should().HaveCount(2);
        results.Should().Contain(j => j.Source == "Google Jobs");
        results.Should().Contain(j => j.Source == "Adzuna");
    }

    [Fact]
    public async Task SearchAsync_OneSourceFails_OtherStillReturns()
    {
        var failingSource = new Mock<IJobSearchSource>();
        failingSource.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new HttpRequestException("API unavailable"));

        var workingSource = new Mock<IJobSearchSource>();
        workingSource.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync([new JobListing { ExternalId = "1", Source = "Adzuna" }]);

        var sut = new CompositeJobSearchService(
            [failingSource.Object, workingSource.Object],
            NullLogger<CompositeJobSearchService>.Instance);

        var results = await sut.SearchAsync("test", "US");

        results.Should().HaveCount(1);
        results[0].Source.Should().Be("Adzuna");
    }

    [Fact]
    public async Task SearchAsync_BothSourcesFail_ReturnsEmpty()
    {
        var source1 = new Mock<IJobSearchSource>();
        source1.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new HttpRequestException("API 1 down"));

        var source2 = new Mock<IJobSearchSource>();
        source2.Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new HttpRequestException("API 2 down"));

        var sut = new CompositeJobSearchService(
            [source1.Object, source2.Object],
            NullLogger<CompositeJobSearchService>.Instance);

        var results = await sut.SearchAsync("test", "US");

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_NoSources_ReturnsEmpty()
    {
        var sut = new CompositeJobSearchService(
            [],
            NullLogger<CompositeJobSearchService>.Instance);

        var results = await sut.SearchAsync("test", "US");

        results.Should().BeEmpty();
    }
}
