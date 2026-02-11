using System.Net;
using System.Net.Http.Json;
using CareerAgent.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CareerAgent.Api.Tests.Endpoints;

public class ProfileEndpointTests
{
    private static WebApplicationFactory<Program> CreateFactory() => new();

    [Fact]
    public async Task GetProfile_ReturnsSeededDefaults()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/profile");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<SearchProfileDto>();
        profile.Should().NotBeNull();
        profile!.Query.Should().NotBeNullOrEmpty();
        profile.Location.Should().NotBeNullOrEmpty();
        profile.RequiredSkills.Should().NotBeEmpty();
        profile.PreferredSkills.Should().NotBeEmpty();
        profile.TitleKeywords.Should().NotBeEmpty();
        profile.NegativeTitleKeywords.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PutProfile_PersistsAndReturnsUpdated()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Ensure profile exists
        await client.GetAsync("/api/profile");

        var update = new SearchProfileUpdateRequest(
            Query: "Python Developer",
            Location: "San Francisco",
            RadiusMiles: 25,
            RemoteOnly: true,
            RequiredSkills: ["Python", "React", "AWS"],
            PreferredSkills: ["Docker", "Kubernetes"],
            TitleKeywords: ["Senior Python Developer"],
            NegativeTitleKeywords: ["Junior"]
        );

        var putResponse = await client.PutAsJsonAsync("/api/profile", update);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saved = await putResponse.Content.ReadFromJsonAsync<SearchProfileDto>();
        saved.Should().NotBeNull();
        saved!.Query.Should().Be("Python Developer");
        saved.Location.Should().Be("San Francisco");
        saved.RadiusMiles.Should().Be(25);
        saved.RemoteOnly.Should().BeTrue();
        saved.RequiredSkills.Should().BeEquivalentTo(["Python", "React", "AWS"]);
        saved.PreferredSkills.Should().BeEquivalentTo(["Docker", "Kubernetes"]);
        saved.TitleKeywords.Should().BeEquivalentTo(["Senior Python Developer"]);
        saved.NegativeTitleKeywords.Should().BeEquivalentTo(["Junior"]);
    }

    [Fact]
    public async Task GetProfile_AfterPut_ReflectsChanges()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // Ensure profile exists
        await client.GetAsync("/api/profile");

        var update = new SearchProfileUpdateRequest(
            Query: "Go Backend Engineer",
            Location: "Austin, TX",
            RadiusMiles: 40,
            RemoteOnly: false,
            RequiredSkills: ["Go", "Kubernetes"],
            PreferredSkills: ["gRPC"],
            TitleKeywords: ["Backend Engineer"],
            NegativeTitleKeywords: ["Intern"]
        );

        await client.PutAsJsonAsync("/api/profile", update);

        var getResponse = await client.GetAsync("/api/profile");
        var profile = await getResponse.Content.ReadFromJsonAsync<SearchProfileDto>();

        profile.Should().NotBeNull();
        profile!.Query.Should().Be("Go Backend Engineer");
        profile.RequiredSkills.Should().BeEquivalentTo(["Go", "Kubernetes"]);
    }
}
