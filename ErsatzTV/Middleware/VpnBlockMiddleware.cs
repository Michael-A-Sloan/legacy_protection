using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Middleware;

public class VpnBlockMiddleware(
    RequestDelegate next,
    IAnonymousIpDetectionService anonymousIpDetectionService,
    IAdminLoginProtectionService loginProtectionService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!AdminProtectionHelper.IsEnabled || !AdminVpnBlockSettings.IsEnabled)
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

        AnonymousIpLookupResult lookup =
            AnonymousIpDetectionHelper.LookupClientIp(anonymousIpDetectionService, clientIp);
        if (!lookup.IsBlocked)
        {
            await next(context);
            return;
        }

        string userAgent = context.Request.Headers.UserAgent.ToString();

        await loginProtectionService.RecordAttemptAsync(
            clientIp,
            string.Empty,
            false,
            userAgent,
            lookup.DenyReason,
            AdminLoginAttemptKind.AccessDenied,
            path,
            cancellationToken: context.RequestAborted);

        AdminSecurityLog.Warning(
            "Blocked VPN/proxy access from {RemoteIP} to {Path}: {Reason}",
            clientIp.Display,
            path,
            lookup.DenyReason);

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
                    <p>Access from this network is not permitted.</p>
                </div>
            </body>
            </html>
            """);
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
