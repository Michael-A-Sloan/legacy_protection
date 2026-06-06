using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Middleware;

public class BannedIpBlockMiddleware(
    RequestDelegate next,
    IAdminLoginProtectionService loginProtectionService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!AdminProtectionHelper.IsEnabled)
        {
            await next(context);
            return;
        }

        string path = context.Request.Path.Value ?? "/";
        if (IsIgnoredPath(path))
        {
            await next(context);
            return;
        }

        IpAddressPair clientIp = ClientIpHelper.GetClientIpInfo(context);
        if (ProtectedIpExemption.IsExempt(clientIp))
        {
            await next(context);
            return;
        }

        if (!await loginProtectionService.IsIpBannedAsync(clientIp, context.RequestAborted))
        {
            await next(context);
            return;
        }

        if (path.StartsWith("/access-denied/banned", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        context.Response.Redirect("/access-denied/banned");
    }

    private static bool IsIgnoredPath(string path) =>
        path.StartsWith("/iptv", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/discover.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/device.xml", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lineup_status.json", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
}
