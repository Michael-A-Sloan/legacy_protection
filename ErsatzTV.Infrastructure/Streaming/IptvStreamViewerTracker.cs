using System.Collections.Concurrent;
using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Infrastructure.Streaming;

public class IptvStreamViewerTracker : IIptvStreamViewerTracker
{
    private readonly ConcurrentDictionary<(string Channel, string ClientId), DateTimeOffset> _lastActivity = new();

    public void RecordActivity(string channelNumber, string clientId)
    {
        if (string.IsNullOrWhiteSpace(channelNumber) || string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        _lastActivity[(channelNumber.Trim(), clientId.Trim())] = DateTimeOffset.UtcNow;
    }

    public IReadOnlyDictionary<string, int> GetActiveViewerCountsByChannel(TimeSpan activityWindow)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - activityWindow;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<(string Channel, string ClientId), DateTimeOffset> entry in _lastActivity)
        {
            if (entry.Value < cutoff)
            {
                _lastActivity.TryRemove(entry.Key, out _);
                continue;
            }

            counts.TryGetValue(entry.Key.Channel, out int count);
            counts[entry.Key.Channel] = count + 1;
        }

        return counts;
    }

    public int GetActiveViewerCount(string channelNumber, TimeSpan activityWindow)
    {
        if (string.IsNullOrWhiteSpace(channelNumber))
        {
            return 0;
        }

        DateTimeOffset cutoff = DateTimeOffset.UtcNow - activityWindow;
        int count = 0;

        foreach (KeyValuePair<(string Channel, string ClientId), DateTimeOffset> entry in _lastActivity)
        {
            if (!string.Equals(entry.Key.Channel, channelNumber, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (entry.Value < cutoff)
            {
                _lastActivity.TryRemove(entry.Key, out _);
                continue;
            }

            count++;
        }

        return count;
    }
}
