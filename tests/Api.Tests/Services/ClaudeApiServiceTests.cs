using System.Net;
using System.Text.Json;
using CareerAgent.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class ClaudeApiServiceTests
{
    // ==================== Prompt Building ====================

    [Fact]
    public void BuildPrompt_IncludesAllSections()
    {
        var prompt = ClaudeApiService.BuildPrompt(
            "# John Doe\nSoftware Engineer",
            "Looking for C# and .NET experience",
            "Senior Software Engineer",
            "Acme Corp");

        prompt.Should().Contain("John Doe");
        prompt.Should().Contain("Senior Software Engineer");
        prompt.Should().Contain("Acme Corp");
        prompt.Should().Contain("C# and .NET");
        prompt.Should().Contain("---RESUME_START---");
        prompt.Should().Contain("---RESUME_END---");
        prompt.Should().Contain("---COVER_LETTER_START---");
        prompt.Should().Contain("---COVER_LETTER_END---");
    }

    // ==================== Response Parsing ====================

    [Fact]
    public void ParseResponse_ExtractsBothSections()
    {
        var response = """
            Some preamble text

            ---RESUME_START---
            # John Doe
            ## Senior Software Engineer

            - 10 years experience with C# and .NET
            ---RESUME_END---

            ---COVER_LETTER_START---
            Dear Hiring Manager,

            I am writing to express my interest...
            ---COVER_LETTER_END---
            """;

        var (resume, coverLetter) = ClaudeApiService.ParseResponse(response);

        resume.Should().Contain("John Doe");
        resume.Should().Contain("10 years experience");
        coverLetter.Should().Contain("Dear Hiring Manager");
        coverLetter.Should().Contain("express my interest");
    }

    [Fact]
    public void ParseResponse_MissingMarkers_FallsBackToFullResponse()
    {
        var response = "# John Doe\n## Engineer\n- Experienced developer";

        var (resume, coverLetter) = ClaudeApiService.ParseResponse(response);

        resume.Should().Contain("John Doe");
        coverLetter.Should().BeEmpty();
    }

    [Fact]
    public void ParseResponse_OnlyResumeMarkers_ExtractsResume()
    {
        var response = """
            ---RESUME_START---
            # Resume content here
            ---RESUME_END---
            """;

        var (resume, coverLetter) = ClaudeApiService.ParseResponse(response);

        resume.Should().Contain("Resume content here");
        coverLetter.Should().BeEmpty();
    }

    // ==================== API Call with mocked HttpClient ====================

    [Fact]
    public async Task TailorResumeAsync_SendsCorrectRequest_ParsesResponse()
    {
        var claudeResponse = new ClaudeResponse
        {
            Id = "msg_123",
            Model = "claude-sonnet-4-5-20250929",
            Content =
            [
                new ClaudeContentBlock
                {
                    Type = "text",
                    Text = """
                        ---RESUME_START---
                        # Tailored Resume
                        ## Senior .NET Developer
                        - Built microservices with C# and .NET 8
                        ---RESUME_END---

                        ---COVER_LETTER_START---
                        Dear Hiring Manager,

                        I am excited to apply for the Senior .NET Developer position at TechCo.
                        ---COVER_LETTER_END---
                        """
                }
            ],
            Usage = new ClaudeUsage { InputTokens = 500, OutputTokens = 300 }
        };

        var json = JsonSerializer.Serialize(claudeResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:ApiKey"] = "test-api-key",
                ["Claude:Model"] = "claude-sonnet-4-5-20250929",
                ["Claude:MaxTokens"] = "4096"
            })
            .Build();

        var logger = NullLogger<ClaudeApiService>.Instance;

        var service = new ClaudeApiService(httpClient, config, logger);

        var result = await service.TailorResumeAsync(
            "# My Resume\n- C# developer",
            "Looking for .NET experience",
            "Senior .NET Developer",
            "TechCo");

        result.TailoredResumeMarkdown.Should().Contain("Tailored Resume");
        result.TailoredResumeMarkdown.Should().Contain("microservices");
        result.CoverLetterMarkdown.Should().Contain("Dear Hiring Manager");
        result.FullPrompt.Should().NotBeNullOrEmpty();
        result.FullResponse.Should().NotBeNullOrEmpty();

        // Verify correct headers were sent
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.GetValues("x-api-key").Should().Contain("test-api-key");
        handler.LastRequest.Headers.GetValues("anthropic-version").Should().Contain("2023-06-01");
    }

    [Fact]
    public async Task TailorResumeAsync_NoApiKey_ThrowsInvalidOperation()
    {
        var httpClient = new HttpClient();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:ApiKey"] = ""
            })
            .Build();

        var logger = NullLogger<ClaudeApiService>.Instance;

        var service = new ClaudeApiService(httpClient, config, logger);

        var act = () => service.TailorResumeAsync("resume", "desc", "title", "company");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Fact]
    public async Task TailorResumeAsync_ApiError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler(
            """{"error": {"type": "invalid_request_error", "message": "Invalid API key"}}""",
            HttpStatusCode.Unauthorized);
        var httpClient = new HttpClient(handler);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Claude:ApiKey"] = "bad-key"
            })
            .Build();

        var logger = NullLogger<ClaudeApiService>.Instance;

        var service = new ClaudeApiService(httpClient, config, logger);

        var act = () => service.TailorResumeAsync("resume", "desc", "title", "company");
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
