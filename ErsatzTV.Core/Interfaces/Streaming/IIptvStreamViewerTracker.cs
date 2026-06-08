using ErsatzTV.Core.Streaming;

namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IIptvStreamViewerTracker
{
    TimeSpan ActivityWindow { get; }

    void RecordActivity(
        string channelNumber,
        string clientId,
        string ipAddress,
        string ipAddressV4,
        string ipAddressV6,
        string userAgent);

    IReadOnlyDictionary<string, int> GetActiveViewerCountsByChannel(TimeSpan activityWindow);

    int GetActiveViewerCount(string channelNumber, TimeSpan activityWindow);

    IReadOnlyList<IptvStreamViewerSession> GetActiveViewers(TimeSpan activityWindow);

    void RemoveSessionsMatchingIp(string ipAddress);
}
