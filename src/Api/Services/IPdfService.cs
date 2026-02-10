namespace CareerAgent.Api.Services;

public interface IPdfService
{
    byte[] GeneratePdf(string markdown, string title);
}
