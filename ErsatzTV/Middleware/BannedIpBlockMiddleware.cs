using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

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
        if (AdminProtectionPaths.IsIgnoredPath(path))
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
}
