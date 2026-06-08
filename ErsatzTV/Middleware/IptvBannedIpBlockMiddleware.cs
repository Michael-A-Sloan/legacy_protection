using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Middleware;

public class IptvBannedIpBlockMiddleware(
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
        if (!AdminProtectionPaths.IsIptvPath(path))
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

        if (await loginProtectionService.IsIpBannedForIptvAsync(clientIp, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied.");
            return;
        }

        await next(context);
    }
}
