using System.Collections.Concurrent;
using System.Text.Json;

namespace CareerAgent.Api.Services;

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private readonly ConcurrentDictionary<string, GeocodingResult?> _cache = new();
    private readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private DateTime _lastRequest = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NominatimGeocodingService(HttpClient httpClient, ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CareerAgent/1.0 (job-search-tool)");
        _logger = logger;
    }

    public async Task<GeocodingResult?> GeocodeAsync(string address)
    {
        var key = address.Trim().ToLowerInvariant();

        if (_cache.TryGetValue(key, out var cached))
            return cached;

        await _rateLimiter.WaitAsync();
        try
        {
            // Re-check cache after acquiring semaphore
            if (_cache.TryGetValue(key, out cached))
                return cached;

            // Enforce 1 req/sec rate limit per Nominatim policy
            var elapsed = DateTime.UtcNow - _lastRequest;
            if (elapsed.TotalMilliseconds < 1100)
                await Task.Delay(1100 - (int)elapsed.TotalMilliseconds);

            var url = $"/search?q={Uri.EscapeDataString(address)}&format=json&limit=1&countrycodes=us";
            _lastRequest = DateTime.UtcNow;

            var response = await _httpClient.GetStringAsync(url);
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(response, JsonOptions);

            GeocodingResult? result = null;
            if (results is { Count: > 0 })
            {
                var first = results[0];
                if (double.TryParse(first.Lat, out var lat) && double.TryParse(first.Lon, out var lon))
                {
                    result = new GeocodingResult(lat, lon, first.DisplayName ?? address);
                    _logger.LogDebug("Geocoded '{Address}' -> ({Lat}, {Lon})", address, lat, lon);
                }
            }

            if (result is null)
                _logger.LogWarning("Failed to geocode '{Address}'", address);

            _cache[key] = result;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Geocoding error for '{Address}'", address);
            _cache[key] = null;
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private class NominatimResult
    {
        public string? Lat { get; set; }
        public string? Lon { get; set; }
        public string? DisplayName { get; set; }
    }
}
