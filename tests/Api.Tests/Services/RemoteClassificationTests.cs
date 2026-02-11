using CareerAgent.Api.Services;
using FluentAssertions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class RemoteClassificationTests
{
    [Theory]
    [InlineData("Remote", "", true)]
    [InlineData("Anywhere", "", true)]
    [InlineData("United States", "", true)]
    [InlineData("remote", "", true)]
    [InlineData("US", "", true)]
    [InlineData("USA", "", true)]
    [InlineData("Work from home", "", true)]
    [InlineData("New York, NY (Remote)", "", true)]
    public void ClassifyRemote_RemoteLocations_ReturnsTrue(string location, string description, bool expected)
    {
        RemoteClassifier.ClassifyRemote(location, description).Should().Be(expected);
    }

    [Theory]
    [InlineData("New York, NY", "", false)]
    [InlineData("San Francisco, CA", "", false)]
    [InlineData("Detroit, MI", "", false)]
    [InlineData("Longmont, CO", "", false)]
    public void ClassifyRemote_PhysicalLocations_ReturnsFalse(string location, string description, bool expected)
    {
        RemoteClassifier.ClassifyRemote(location, description).Should().Be(expected);
    }

    [Theory]
    [InlineData("Chicago, IL", "This is a fully remote position with flexible hours", true)]
    [InlineData("Boston, MA", "100% remote role for US-based engineers", true)]
    [InlineData("Austin, TX", "Work remotely from anywhere in the US", true)]
    [InlineData("Seattle, WA", "Remote opportunity for senior developers", true)]
    public void ClassifyRemote_DescriptionContainsRemoteSignals_ReturnsTrue(string location, string description, bool expected)
    {
        RemoteClassifier.ClassifyRemote(location, description).Should().Be(expected);
    }

    [Theory]
    [InlineData("Denver, CO", "On-site position in our downtown office")]
    [InlineData("Miami, FL", "Hybrid role, 3 days in office")]
    public void ClassifyRemote_NoRemoteSignals_ReturnsFalse(string location, string description)
    {
        RemoteClassifier.ClassifyRemote(location, description).Should().BeFalse();
    }

    [Theory]
    [InlineData("Software Engineer - Remote")]
    [InlineData("Software Engineer - REMOTE")]
    [InlineData("Software Engineer (Remote)")]
    [InlineData("Remote Software Engineer")]
    [InlineData("Senior Dev - Work From Home")]
    public void ClassifyRemote_TitleContainsRemoteSignals_ReturnsTrue(string title)
    {
        RemoteClassifier.ClassifyRemote("Auburn Hills, MI", "On-site office job", title).Should().BeTrue();
    }

    [Theory]
    [InlineData("Senior Software Engineer")]
    [InlineData("Full Stack Developer")]
    public void ClassifyRemote_TitleWithoutRemoteSignals_ReturnsFalse(string title)
    {
        RemoteClassifier.ClassifyRemote("Auburn Hills, MI", "On-site office job", title).Should().BeFalse();
    }
}
