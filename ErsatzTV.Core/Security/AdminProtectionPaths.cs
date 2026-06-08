namespace ErsatzTV.Core.Security;

public static class AdminProtectionPaths
{
    public static bool IsIptvPath(string path) =>
        path.StartsWith("/iptv", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/discover.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/device.xml", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup_status.json", StringComparison.OrdinalIgnoreCase);

    public static bool IsIgnoredPath(string path) =>
        IsIptvPath(path) ||
        path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase);

    public static bool IsBlockedDiscoveryPath(string path) =>
        path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/site.webmanifest", StringComparison.OrdinalIgnoreCase);

    public static bool ShouldApplyPrivacyHeaders(string path) =>
        !IsIptvPath(path) && !IsIgnoredPath(path);
}
