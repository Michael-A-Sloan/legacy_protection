namespace ErsatzTV.Core.Streaming;

public sealed class IptvStreamViewerSession
{
    public string ChannelNumber { get; init; }

    public string ClientId { get; init; }

    public string IpAddress { get; init; }

    public string IpAddressV4 { get; init; }

    public string IpAddressV6 { get; init; }

    public string UserAgent { get; init; }

    public DateTimeOffset LastActivityUtc { get; init; }
}
