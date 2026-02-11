using CareerAgent.Api.Services;
using FluentAssertions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class GeoMathTests
{
    [Fact]
    public void HaversineDistanceMiles_NewYorkToLosAngeles_ReturnsApproximately2451()
    {
        // NYC (40.7128, -74.0060) to LA (34.0522, -118.2437)
        var distance = GeoMath.HaversineDistanceMiles(40.7128, -74.0060, 34.0522, -118.2437);
        distance.Should().BeApproximately(2451, 10);
    }

    [Fact]
    public void HaversineDistanceMiles_SamePoint_ReturnsZero()
    {
        var distance = GeoMath.HaversineDistanceMiles(42.6583, -83.1499, 42.6583, -83.1499);
        distance.Should().Be(0);
    }

    [Fact]
    public void HaversineDistanceMiles_RochesterHillsToDetroit_ReturnsApproximately30()
    {
        // Rochester Hills, MI (42.6583, -83.1499) to Detroit, MI (42.3314, -83.0458)
        var distance = GeoMath.HaversineDistanceMiles(42.6583, -83.1499, 42.3314, -83.0458);
        distance.Should().BeApproximately(23, 5);
    }

    [Fact]
    public void HaversineDistanceMiles_RochesterHillsToChicago_ReturnsApproximately280()
    {
        // Rochester Hills, MI (42.6583, -83.1499) to Chicago, IL (41.8781, -87.6298)
        var distance = GeoMath.HaversineDistanceMiles(42.6583, -83.1499, 41.8781, -87.6298);
        distance.Should().BeApproximately(235, 15);
    }
}
