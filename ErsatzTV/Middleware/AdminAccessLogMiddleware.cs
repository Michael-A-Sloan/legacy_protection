using ErsatzTV.Authorization;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Middleware;

public class AdminAccessLogMiddleware(RequestDelegate next)
{
    private static readonly System.Collections.Generic.HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (!AdminProtectionHelper.IsEnabled)
        {
            return;
        }

        string method = context.Request.Method;
        if (!MutatingMethods.Contains(method))
        {
            return;
        }

        string clientIp = ClientIpHelper.GetClientIpInfo(context).Display;
        string user = context.User.Identity?.IsAuthenticated == true
            ? context.User.Identity.Name ?? "authenticated"
            : "anonymous";

        int statusCode = context.Response.StatusCode;
        string path = context.Request.Path.ToUriComponent();

        if (statusCode is >= 200 and < 300)
        {
            AdminSecurityLog.Information(
                "Admin change: {Method} {Path} by {User} from {RemoteIP} -> {StatusCode}",
                method,
                path,
                user,
                clientIp,
                statusCode);
        }
        else if (statusCode is 401 or 403)
        {
            AdminSecurityLog.Warning(
                "Blocked admin change: {Method} {Path} by {User} from {RemoteIP} -> {StatusCode}",
                method,
                path,
                user,
                clientIp,
                statusCode);
        }
    }
}
