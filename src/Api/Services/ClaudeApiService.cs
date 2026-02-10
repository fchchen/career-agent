using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareerAgent.Api.Services;

public class ClaudeApiService : IClaudeApiService
{
    private const string ApiBaseUrl = "https://api.anthropic.com/v1/messages";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeApiService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TailorResponse> TailorResumeAsync(string resumeMarkdown, string jobDescription, string jobTitle, string company)
    {
        var apiKey = _configuration["Claude:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Claude API key not configured");
        var model = _configuration["Claude:Model"] ?? "claude-sonnet-4-5-20250929";
        var maxTokens = _configuration.GetValue("Claude:MaxTokens", 4096);

        var prompt = BuildPrompt(resumeMarkdown, jobDescription, jobTitle, company);

        var request = new ClaudeRequest
        {
            Model = model,
            MaxTokens = maxTokens,
            Messages =
            [
                new ClaudeMessage { Role = "user", Content = prompt }
            ],
            System = "You are an expert resume writer and career coach. You tailor resumes to match specific job descriptions while maintaining truthfulness. You respond ONLY with the requested structured output, no commentary."
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        _logger.LogInformation("Calling Claude API for resume tailoring: {JobTitle} at {Company}", jobTitle, company);

        var response = await _httpClient.PostAsync(ApiBaseUrl, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Claude API error {StatusCode}: {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Claude API returned {response.StatusCode}: {responseBody}");
        }

        var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseBody, JsonOptions);
        var responseText = claudeResponse?.Content?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Empty response from Claude API");

        _logger.LogInformation("Claude API response received, {Length} chars", responseText.Length);

        var (tailoredResume, coverLetter) = ParseResponse(responseText);

        return new TailorResponse(tailoredResume, coverLetter, prompt, responseText);
    }

    internal static string BuildPrompt(string resumeMarkdown, string jobDescription, string jobTitle, string company)
    {
        return $"""
            I need you to tailor my resume and write a cover letter for a specific job.

            ## My Current Resume (Markdown)

            {resumeMarkdown}

            ## Target Job

            **Title:** {jobTitle}
            **Company:** {company}

            **Job Description:**
            {jobDescription}

            ## Instructions

            1. **Tailored Resume**: Rewrite my resume in Markdown format, optimized for this specific role:
               - Reorder and emphasize skills/experience that match the job description
               - Use keywords from the job description naturally
               - Keep all information truthful — do not invent experience or skills I don't have
               - Maintain professional formatting with clear sections

            2. **Cover Letter**: Write a concise, compelling cover letter in Markdown format:
               - Address why I'm a strong fit for this specific role
               - Reference 2-3 specific achievements from my resume that align with the job
               - Keep it under 400 words
               - Professional but not overly formal tone

            ## Required Output Format

            Respond with EXACTLY this structure (including the delimiter markers):

            ---RESUME_START---
            [Your tailored resume in Markdown here]
            ---RESUME_END---

            ---COVER_LETTER_START---
            [Your cover letter in Markdown here]
            ---COVER_LETTER_END---
            """;
    }

    internal static (string Resume, string CoverLetter) ParseResponse(string response)
    {
        var resume = ExtractSection(response, "---RESUME_START---", "---RESUME_END---");
        var coverLetter = ExtractSection(response, "---COVER_LETTER_START---", "---COVER_LETTER_END---");

        if (string.IsNullOrWhiteSpace(resume))
            resume = response; // Fallback: use entire response as resume

        return (resume.Trim(), coverLetter.Trim());
    }

    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var startIndex = text.IndexOf(startMarker, StringComparison.Ordinal);
        if (startIndex < 0) return string.Empty;

        startIndex += startMarker.Length;
        var endIndex = text.IndexOf(endMarker, startIndex, StringComparison.Ordinal);
        if (endIndex < 0) return text[startIndex..].Trim();

        return text[startIndex..endIndex].Trim();
    }
}

// Claude API request/response models
public class ClaudeRequest
{
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public string? System { get; set; }
    public List<ClaudeMessage> Messages { get; set; } = [];
}

public class ClaudeMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ClaudeResponse
{
    public string? Id { get; set; }
    public string? Model { get; set; }
    public List<ClaudeContentBlock>? Content { get; set; }
    public ClaudeUsage? Usage { get; set; }
}

public class ClaudeContentBlock
{
    public string? Type { get; set; }
    public string? Text { get; set; }
}

public class ClaudeUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
