using Markdig;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CareerAgent.Api.Services;

public class PdfService : IPdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(string markdown, string title)
    {
        var htmlContent = Markdown.ToHtml(markdown, new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build());

        var plainSections = ParseMarkdownSections(markdown);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.MarginHorizontal(50);
                page.MarginVertical(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Content().Column(col =>
                {
                    foreach (var section in plainSections)
                    {
                        if (section.IsHeading)
                        {
                            col.Item().PaddingTop(section.Level == 1 ? 0 : 10).PaddingBottom(4).Text(section.Text)
                                .FontSize(section.Level switch
                                {
                                    1 => 18,
                                    2 => 14,
                                    _ => 12
                                })
                                .Bold();

                            if (section.Level <= 2)
                            {
                                col.Item().PaddingBottom(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
                            }
                        }
                        else if (section.IsBullet)
                        {
                            col.Item().PaddingLeft(15).PaddingBottom(2).Row(row =>
                            {
                                row.AutoItem().Text("• ").FontSize(10);
                                row.RelativeItem().Text(section.Text).FontSize(10);
                            });
                        }
                        else if (!string.IsNullOrWhiteSpace(section.Text))
                        {
                            col.Item().PaddingBottom(4).Text(section.Text).FontSize(10);
                        }
                        else
                        {
                            col.Item().PaddingBottom(6);
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    internal static List<PdfSection> ParseMarkdownSections(string markdown)
    {
        var sections = new List<PdfSection>();
        var lines = markdown.Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith("### "))
                sections.Add(new PdfSection(line[4..].Trim(), true, 3, false));
            else if (line.StartsWith("## "))
                sections.Add(new PdfSection(line[3..].Trim(), true, 2, false));
            else if (line.StartsWith("# "))
                sections.Add(new PdfSection(line[2..].Trim(), true, 1, false));
            else if (line.StartsWith("- ") || line.StartsWith("* "))
                sections.Add(new PdfSection(line[2..].Trim(), false, 0, true));
            else if (line.StartsWith("  - ") || line.StartsWith("  * "))
                sections.Add(new PdfSection(line[4..].Trim(), false, 0, true));
            else
                sections.Add(new PdfSection(CleanMarkdownFormatting(line), false, 0, false));
        }

        return sections;
    }

    private static string CleanMarkdownFormatting(string text)
    {
        // Strip bold/italic markers for plain text rendering
        return text
            .Replace("**", "")
            .Replace("__", "")
            .Replace("*", "")
            .Replace("_", "")
            .Trim();
    }
}

public record PdfSection(string Text, bool IsHeading, int Level, bool IsBullet);
