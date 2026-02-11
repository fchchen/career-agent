using CareerAgent.Api.Services;
using CareerAgent.Shared.Constants;
using CareerAgent.Shared.DTOs;
using CareerAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CareerAgent.Api.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .WithTags("Profile");

        group.MapGet("/", GetProfile)
            .Produces<SearchProfileDto>();

        group.MapPut("/", UpdateProfile)
            .Produces<SearchProfileDto>();

        return app;
    }

    private static async Task<IResult> GetProfile(IStorageService storageService)
    {
        var profile = await storageService.GetSearchProfileAsync();
        if (profile is null)
        {
            // Seed a default profile matching user's background
            profile = new SearchProfile
            {
                Name = "Default",
                Query = SearchDefaults.DefaultQuery,
                Location = "Rochester Hills, MI 48307",
                RadiusMiles = SearchDefaults.DefaultRadiusMiles,
                RequiredSkills = [".NET", "C#", "Angular", "TypeScript", "SQL Server", "Azure", "AWS"],
                PreferredSkills = SkillTaxonomy.StrongSkills.ToList(),
                TitleKeywords = SearchDefaults.DefaultTitleKeywords.ToList(),
                NegativeTitleKeywords = SearchDefaults.NegativeTitleKeywords.ToList(),
            };
            profile = await storageService.UpsertSearchProfileAsync(profile);
        }

        return Results.Ok(MapToDto(profile));
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] SearchProfileUpdateRequest request,
        IStorageService storageService)
    {
        var existing = await storageService.GetSearchProfileAsync();
        var profile = existing ?? new SearchProfile();

        profile.Query = request.Query;
        profile.Location = request.Location;
        profile.RadiusMiles = request.RadiusMiles;
        profile.RemoteOnly = request.RemoteOnly;
        profile.RequiredSkills = request.RequiredSkills;
        profile.PreferredSkills = request.PreferredSkills;
        profile.TitleKeywords = request.TitleKeywords;
        profile.NegativeTitleKeywords = request.NegativeTitleKeywords;

        var saved = await storageService.UpsertSearchProfileAsync(profile);
        return Results.Ok(MapToDto(saved));
    }

    private static SearchProfileDto MapToDto(SearchProfile p) => new(
        p.Id, p.Name, p.Query, p.Location, p.RadiusMiles, p.RemoteOnly,
        p.RequiredSkills, p.PreferredSkills, p.TitleKeywords, p.NegativeTitleKeywords,
        p.CreatedAt);
}
