namespace ErsatzTV.Core.Interfaces.Streaming;

public interface IIptvStreamViewerTracker
{
    void RecordActivity(string channelNumber, string clientId);

    IReadOnlyDictionary<string, int> GetActiveViewerCountsByChannel(TimeSpan activityWindow);

    int GetActiveViewerCount(string channelNumber, TimeSpan activityWindow);
}
