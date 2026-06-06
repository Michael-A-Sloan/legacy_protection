using System.Text.RegularExpressions;

namespace ErsatzTV.Core.Streaming;

public static partial class IptvStreamPathParser
{
    [GeneratedRegex(
        @"^/iptv/(?:channel|hdhr/channel)/([^/.]+)(?:\.|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChannelStreamPathRegex();

    [GeneratedRegex(
        @"^/iptv/session/([^/]+)/",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SessionStreamPathRegex();

    public static bool IsStreamActivityPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (path.Contains("/iptv/logos", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.StartsWith("/iptv/channel/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/iptv/session/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/iptv/hdhr/channel/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetChannelNumber(string path, out string channelNumber)
    {
        channelNumber = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        Match channelMatch = ChannelStreamPathRegex().Match(path);
        if (channelMatch.Success)
        {
            channelNumber = channelMatch.Groups[1].Value;
            return true;
        }

        Match sessionMatch = SessionStreamPathRegex().Match(path);
        if (sessionMatch.Success)
        {
            channelNumber = sessionMatch.Groups[1].Value;
            return true;
        }

        return false;
    }
}
