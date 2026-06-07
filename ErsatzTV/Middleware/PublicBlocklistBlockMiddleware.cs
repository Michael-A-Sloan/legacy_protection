using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Middleware;

public class PublicBlocklistBlockMiddleware(
    RequestDelegate next,
    IPublicBlocklistService publicBlocklistService,
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

        PublicBlocklistMatchResult match =
            await publicBlocklistService.MatchClientIpAsync(clientIp, context.RequestAborted);
        if (!match.IsBlocked)
        {
            await next(context);
            return;
        }

        string denyReason = $"IP address matched public blocklist: {match.ListName}.";
        string userAgent = context.Request.Headers.UserAgent.ToString();

        await loginProtectionService.RecordAttemptAsync(
            clientIp,
            string.Empty,
            false,
            userAgent,
            denyReason,
            AdminLoginAttemptKind.AccessDenied,
            path,
            cancellationToken: context.RequestAborted);

        AdminSecurityLog.Warning(
            "Blocked public blocklist IP {RemoteIP} from admin access to {Path}: {ListName}",
            clientIp.Display,
            path,
            match.ListName);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8"/>
                <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                <title>Access Denied</title>
                <style>
                    body { margin: 0; min-height: 100vh; display: flex; align-items: center; justify-content: center;
                           background: #272727; color: rgba(255,255,255,.9); font-family: Roboto, Helvetica, Arial, sans-serif; }
                    .card { max-width: 420px; padding: 2rem; border-radius: 8px; background: #1f1f1f; text-align: center; }
                </style>
            </head>
            <body>
                <div class="card">
                    <h1>Access Denied</h1>
                    <p>Access from this IP address is not permitted.</p>
                </div>
            </body>
            </html>
            """);
    }
}
