using ErsatzTV.Core.Interfaces.Streaming;

namespace ErsatzTV.Application.Streaming;

public class GetActiveIptvViewerCountsHandler(IIptvStreamViewerTracker viewerTracker)
    : IRequestHandler<GetActiveIptvViewerCounts, IReadOnlyDictionary<string, int>>
{
    private static readonly TimeSpan ActivityWindow = TimeSpan.FromSeconds(60);

    public Task<IReadOnlyDictionary<string, int>> Handle(
        GetActiveIptvViewerCounts request,
        CancellationToken cancellationToken) =>
        Task.FromResult(viewerTracker.GetActiveViewerCountsByChannel(ActivityWindow));
}
