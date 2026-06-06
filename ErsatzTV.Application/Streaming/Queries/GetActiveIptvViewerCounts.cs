namespace ErsatzTV.Application.Streaming;

public record GetActiveIptvViewerCounts : IRequest<IReadOnlyDictionary<string, int>>;
