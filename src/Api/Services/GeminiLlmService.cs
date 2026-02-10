using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CareerAgent.Api.Services;

public class GeminiLlmService : ILlmService
{
    private const string ApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiLlmService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiLlmService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiLlmService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private static readonly string[] DefaultModels = ["gemini-2.5-flash", "gemini-2.5-flash-lite", "gemini-3-flash"];

    public async Task<TailorResponse> TailorResumeAsync(string resumeMarkdown, string jobDescription, string jobTitle, string company)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini API key not configured");

        var models = _configuration.GetSection("Gemini:Models").Get<string[]>() ?? DefaultModels;
        var maxOutputTokens = _configuration.GetValue("Gemini:MaxOutputTokens", 4096);

        var prompt = BuildPrompt(resumeMarkdown, jobDescription, jobTitle, company);

        var request = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts = [new GeminiPart { Text = prompt }]
                }
            ],
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = "You are an expert resume writer and career coach. You tailor resumes to match specific job descriptions while maintaining truthfulness. You respond ONLY with the requested structured output, no commentary." }]
            },
            GenerationConfig = new GeminiGenerationConfig
            {
                MaxOutputTokens = maxOutputTokens
            }
        };

        var json = JsonSerializer.Serialize(request, JsonOptions);

        foreach (var model in models)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{ApiBaseUrl}/{model}:generateContent?key={apiKey}";

            _logger.LogInformation("Calling Gemini API ({Model}) for resume tailoring: {JobTitle} at {Company}", model, jobTitle, company);

            var response = await _httpClient.PostAsync(url, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Gemini model {Model} rate limited, trying next model", model);
                continue;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, responseBody);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}: {responseBody}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody, JsonOptions);
            var responseText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("Empty response from Gemini API");

            _logger.LogInformation("Gemini API response received from {Model}, {Length} chars", model, responseText.Length);

            var (tailoredResume, coverLetter) = ParseResponse(responseText);

            return new TailorResponse(tailoredResume, coverLetter, prompt, responseText);
        }

        throw new HttpRequestException("All Gemini models are rate limited. Please try again later.");
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

// Gemini API request/response models
public class GeminiRequest
{
    public List<GeminiContent> Contents { get; set; } = [];
    public GeminiContent? SystemInstruction { get; set; }
    public GeminiGenerationConfig? GenerationConfig { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart> Parts { get; set; } = [];
}

public class GeminiPart
{
    public string? Text { get; set; }
}

public class GeminiGenerationConfig
{
    public int MaxOutputTokens { get; set; }
}

public class GeminiResponse
{
    public List<GeminiCandidate>? Candidates { get; set; }
}

public class GeminiCandidate
{
    public GeminiContent? Content { get; set; }
}
