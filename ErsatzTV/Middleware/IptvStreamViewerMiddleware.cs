using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Streaming;

namespace ErsatzTV.Middleware;

public class IptvStreamViewerMiddleware(RequestDelegate next, IIptvStreamViewerTracker viewerTracker)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        if (IptvStreamPathParser.IsStreamActivityPath(path) &&
            IptvStreamPathParser.TryGetChannelNumber(path, out string channelNumber))
        {
            IpAddressPair clientIp = ClientIpHelper.GetClientIpInfo(context);
            string accessToken = context.Request.Query["access_token"].FirstOrDefault();
            string clientId = IptvViewerClientId.Build(clientIp.Canonical, accessToken);
            string userAgent = context.Request.Headers.UserAgent.ToString();

            viewerTracker.RecordActivity(
                channelNumber,
                clientId,
                clientIp.Canonical,
                clientIp.Ipv4 ?? string.Empty,
                clientIp.Ipv6 ?? string.Empty,
                userAgent);
        }

        await next(context);
    }
}
