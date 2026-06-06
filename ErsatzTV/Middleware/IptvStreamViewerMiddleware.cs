using ErsatzTV.Core.Interfaces.Streaming;
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
            string clientId = GetClientId(context);
            viewerTracker.RecordActivity(channelNumber, clientId);
        }

        await next(context);
    }

    private static string GetClientId(HttpContext context)
    {
        string ip = ClientIpHelper.GetClientIp(context);
        string accessToken = context.Request.Query["access_token"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ip;
        }

        return $"{ip}|{accessToken.Trim()}";
    }
}
