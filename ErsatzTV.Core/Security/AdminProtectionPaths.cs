namespace ErsatzTV.Core.Security;

public static class AdminProtectionPaths
{
    public static bool IsIgnoredPath(string path) =>
        path.StartsWith("/iptv", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/discover.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/device.xml", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup_status.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
}
