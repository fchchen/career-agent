using CareerAgent.Api.Services;
using CareerAgent.Shared.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CareerAgent.Api.Tests.Services;

public class ResumeServiceTests
{
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<IClaudeApiService> _claudeMock = new();
    private readonly Mock<IPdfService> _pdfMock = new();
    private readonly ResumeService _sut;

    public ResumeServiceTests()
    {
        _sut = new ResumeService(_storageMock.Object, _claudeMock.Object, _pdfMock.Object, NullLogger<ResumeService>.Instance);
    }

    [Fact]
    public async Task TailorForJobAsync_HappyPath_ReturnsDocument()
    {
        var job = new JobListing
        {
            Id = 1,
            Title = "Senior .NET Developer",
            Company = "TechCo",
            Description = "Need C# and .NET experience"
        };

        var resume = new MasterResume
        {
            Id = 1,
            RawMarkdown = "# My Resume\n- C# developer"
        };

        _storageMock.Setup(s => s.GetJobByIdAsync(1)).ReturnsAsync(job);
        _storageMock.Setup(s => s.GetMasterResumeAsync(null)).ReturnsAsync(resume);
        _storageMock.Setup(s => s.SaveTailoredDocumentAsync(It.IsAny<TailoredDocument>()))
            .ReturnsAsync((TailoredDocument d) => { d.Id = 42; return d; });

        _claudeMock.Setup(c => c.TailorResumeAsync(
            resume.RawMarkdown, job.Description, job.Title, job.Company))
            .ReturnsAsync(new TailorResponse(
                "# Tailored Resume",
                "Dear Hiring Manager",
                "prompt",
                "response"));

        _pdfMock.Setup(p => p.GeneratePdf(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([0x25, 0x50, 0x44, 0x46]); // %PDF

        var result = await _sut.TailorForJobAsync(1);

        result.Id.Should().Be(42);
        result.JobListingId.Should().Be(1);
        result.MasterResumeId.Should().Be(1);
        result.TailoredResumeMarkdown.Should().Contain("Tailored Resume");
        result.CoverLetterMarkdown.Should().Contain("Dear Hiring Manager");
        result.ClaudePrompt.Should().Be("prompt");
        result.ClaudeResponse.Should().Be("response");

        _storageMock.Verify(s => s.SaveTailoredDocumentAsync(It.IsAny<TailoredDocument>()), Times.Once);
    }

    [Fact]
    public async Task TailorForJobAsync_JobNotFound_ThrowsKeyNotFound()
    {
        _storageMock.Setup(s => s.GetJobByIdAsync(999)).ReturnsAsync((JobListing?)null);

        var act = () => _sut.TailorForJobAsync(999);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*999*");
    }

    [Fact]
    public async Task TailorForJobAsync_NoResume_ThrowsKeyNotFound()
    {
        _storageMock.Setup(s => s.GetJobByIdAsync(1))
            .ReturnsAsync(new JobListing { Id = 1 });
        _storageMock.Setup(s => s.GetMasterResumeAsync(null))
            .ReturnsAsync((MasterResume?)null);

        var act = () => _sut.TailorForJobAsync(1);
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*master resume*");
    }

    [Fact]
    public async Task TailorForJobAsync_PdfFails_StillSavesDocument()
    {
        var job = new JobListing
        {
            Id = 1,
            Title = "Engineer",
            Company = "Co",
            Description = "desc"
        };

        var resume = new MasterResume
        {
            Id = 1,
            RawMarkdown = "# Resume"
        };

        _storageMock.Setup(s => s.GetJobByIdAsync(1)).ReturnsAsync(job);
        _storageMock.Setup(s => s.GetMasterResumeAsync(null)).ReturnsAsync(resume);
        _storageMock.Setup(s => s.SaveTailoredDocumentAsync(It.IsAny<TailoredDocument>()))
            .ReturnsAsync((TailoredDocument d) => { d.Id = 1; return d; });

        _claudeMock.Setup(c => c.TailorResumeAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TailorResponse("resume", "cover", "p", "r"));

        _pdfMock.Setup(p => p.GeneratePdf(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("PDF generation failed"));

        var result = await _sut.TailorForJobAsync(1);

        result.Should().NotBeNull();
        result.PdfPath.Should().BeNull();
        _storageMock.Verify(s => s.SaveTailoredDocumentAsync(It.IsAny<TailoredDocument>()), Times.Once);
    }

    [Fact]
    public async Task TailorForJobAsync_WithSpecificResumeId_UsesCorrectResume()
    {
        var job = new JobListing { Id = 1, Title = "Dev", Company = "Co", Description = "d" };
        var resume = new MasterResume { Id = 5, RawMarkdown = "# Specific Resume" };

        _storageMock.Setup(s => s.GetJobByIdAsync(1)).ReturnsAsync(job);
        _storageMock.Setup(s => s.GetMasterResumeAsync(5)).ReturnsAsync(resume);
        _storageMock.Setup(s => s.SaveTailoredDocumentAsync(It.IsAny<TailoredDocument>()))
            .ReturnsAsync((TailoredDocument d) => { d.Id = 1; return d; });

        _claudeMock.Setup(c => c.TailorResumeAsync(
            "# Specific Resume", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new TailorResponse("r", "c", "p", "resp"));

        _pdfMock.Setup(p => p.GeneratePdf(It.IsAny<string>(), It.IsAny<string>()))
            .Returns([1, 2, 3]);

        var result = await _sut.TailorForJobAsync(1, masterResumeId: 5);

        result.MasterResumeId.Should().Be(5);
        _claudeMock.Verify(c => c.TailorResumeAsync("# Specific Resume", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
