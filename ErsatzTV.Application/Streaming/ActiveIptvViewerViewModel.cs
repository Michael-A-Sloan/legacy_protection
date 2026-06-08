namespace ErsatzTV.Application.Streaming;

public class ActiveIptvViewerViewModel
{
    public string ChannelNumber { get; set; }

    public string ChannelName { get; set; }

    public string IpAddress { get; set; }

    public string IpAddressV4 { get; set; }

    public string IpAddressV6 { get; set; }

    public string UserAgent { get; set; }

    public DateTimeOffset LastActivityUtc { get; set; }

    public bool IsVpn { get; set; }

    public bool IsProxy { get; set; }

    public bool IsTor { get; set; }

    public bool IsLoginBanned { get; set; }

    public bool IsBlacklisted { get; set; }

    public bool CanBan { get; set; }
}
