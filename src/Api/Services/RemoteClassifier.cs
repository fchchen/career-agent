namespace CareerAgent.Api.Services;

public static class RemoteClassifier
{
    public static bool ClassifyRemote(string location, string description, string? title = null)
    {
        var loc = location.ToLowerInvariant();
        var desc = description.ToLowerInvariant();

        // Location-based signals
        if (loc.Contains("remote") || loc == "anywhere" || loc == "united states"
            || loc.Contains("work from home") || loc == "us" || loc == "usa")
            return true;

        // Title-based signals (e.g. Adzuna puts "Remote" in the title)
        if (!string.IsNullOrEmpty(title))
        {
            var t = title.ToLowerInvariant();
            if (t.Contains("remote") || t.Contains("work from home"))
                return true;
        }

        // Description-based signals (only strong indicators)
        var remotePatterns = new[] { "remote position", "fully remote", "100% remote", "work remotely", "remote role", "remote opportunity" };
        return remotePatterns.Any(p => desc.Contains(p));
    }
}
