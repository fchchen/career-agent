namespace CareerAgent.Shared.DTOs;

public record MasterResumeDto(
    int Id,
    string Name,
    string Content,
    string RawMarkdown,
    DateTime UpdatedAt
);

public record MasterResumeUpdateRequest(
    string Content,
    string RawMarkdown
);

public record TailoredDocumentDto(
    int Id,
    int JobListingId,
    string JobTitle,
    string Company,
    string TailoredResumeMarkdown,
    string CoverLetterMarkdown,
    string? PdfPath,
    DateTime CreatedAt
);

public record TailorRequest(
    int? MasterResumeId = null
);
