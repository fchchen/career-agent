namespace CareerAgent.Api.Services;

public record GeocodingResult(double Latitude, double Longitude, string DisplayName);

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(string address);
}
