namespace CareerAgent.Api.Services;

public interface ILlmService
{
    Task<TailorResponse> TailorResumeAsync(string resumeMarkdown, string jobDescription, string jobTitle, string company);
}

public record TailorResponse(
    string TailoredResumeMarkdown,
    string CoverLetterMarkdown,
    string FullPrompt,
    string FullResponse
);
