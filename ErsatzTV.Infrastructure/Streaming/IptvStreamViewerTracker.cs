using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Streaming;

namespace ErsatzTV.Infrastructure.Streaming;

public class IptvStreamViewerTracker : IIptvStreamViewerTracker
{
    public TimeSpan ActivityWindow { get; } = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<(string Channel, string ClientId), IptvStreamViewerSession> _sessions = new();

    public void RecordActivity(
        string channelNumber,
        string clientId,
        string ipAddress,
        string ipAddressV4,
        string ipAddressV6,
        string userAgent)
    {
        if (string.IsNullOrWhiteSpace(channelNumber) || string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        var session = new IptvStreamViewerSession
        {
            ChannelNumber = channelNumber.Trim(),
            ClientId = clientId.Trim(),
            IpAddress = ipAddress?.Trim() ?? string.Empty,
            IpAddressV4 = ipAddressV4?.Trim() ?? string.Empty,
            IpAddressV6 = ipAddressV6?.Trim() ?? string.Empty,
            UserAgent = userAgent?.Trim() ?? string.Empty,
            LastActivityUtc = DateTimeOffset.UtcNow
        };

        _sessions[(session.ChannelNumber, session.ClientId)] = session;
    }

    public IReadOnlyDictionary<string, int> GetActiveViewerCountsByChannel(TimeSpan activityWindow) =>
        GetActiveViewers(activityWindow)
            .GroupBy(session => session.ChannelNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

    public int GetActiveViewerCount(string channelNumber, TimeSpan activityWindow)
    {
        if (string.IsNullOrWhiteSpace(channelNumber))
        {
            return 0;
        }

        return GetActiveViewers(activityWindow)
            .Count(session => string.Equals(session.ChannelNumber, channelNumber, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<IptvStreamViewerSession> GetActiveViewers(TimeSpan activityWindow)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - activityWindow;
        var active = new List<IptvStreamViewerSession>();

        foreach (KeyValuePair<(string Channel, string ClientId), IptvStreamViewerSession> entry in _sessions)
        {
            if (entry.Value.LastActivityUtc < cutoff)
            {
                _sessions.TryRemove(entry.Key, out _);
                continue;
            }

            active.Add(entry.Value);
        }

        return active
            .OrderBy(session => decimal.TryParse(session.ChannelNumber, out decimal number) ? number : decimal.MaxValue)
            .ThenBy(session => session.IpAddress, StringComparer.OrdinalIgnoreCase)
            .ThenBy(session => session.ClientId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void RemoveSessionsMatchingIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return;
        }

        IpAddressPair rule = IpAddressFormatting.FromString(ipAddress.Trim());
        var keysToRemove = new List<(string Channel, string ClientId)>();

        foreach (KeyValuePair<(string Channel, string ClientId), IptvStreamViewerSession> entry in _sessions)
        {
            if (SessionMatchesRule(entry.Value, rule))
            {
                keysToRemove.Add(entry.Key);
            }
        }

        foreach ((string Channel, string ClientId) key in keysToRemove)
        {
            _sessions.TryRemove(key, out _);
        }
    }

    private static bool SessionMatchesRule(IptvStreamViewerSession session, IpAddressPair rule)
    {
        if (!string.IsNullOrWhiteSpace(session.IpAddress) &&
            IpAddressFormatting.MatchesRule(rule.Canonical, IpAddressFormatting.FromString(session.IpAddress)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(session.IpAddressV4) &&
            IpAddressFormatting.MatchesRule(rule.Canonical, new IpAddressPair(session.IpAddressV4, null)))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(session.IpAddressV6) &&
            IpAddressFormatting.MatchesRule(rule.Canonical, new IpAddressPair(null, session.IpAddressV6)))
        {
            return true;
        }

        return false;
    }
}
