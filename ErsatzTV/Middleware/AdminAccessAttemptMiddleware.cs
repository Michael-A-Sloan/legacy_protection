using ErsatzTV.Authorization;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Middleware;

public class AdminAccessAttemptMiddleware(
    RequestDelegate next,
    IAdminLoginProtectionService loginProtectionService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (!AdminProtectionHelper.IsEnabled)
        {
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        IpAddressPair clientIp = ClientIpHelper.GetClientIpInfo(context);
        if (LocalIpExemption.IsExempt(clientIp.Ipv4) ||
            LocalIpExemption.IsExempt(clientIp.Ipv6) ||
            LocalIpExemption.IsExempt(clientIp.Canonical))
        {
            return;
        }

        string path = context.Request.Path.Value ?? "/";
        if (IsIgnoredPath(path))
        {
            return;
        }

        string method = context.Request.Method;
        string userAgent = context.Request.Headers.UserAgent.ToString();

        if (AdminAuthHelper.IsEnabled &&
            HttpMethods.IsGet(method) &&
            path.StartsWith("/login", StringComparison.OrdinalIgnoreCase) &&
            context.Response.StatusCode == StatusCodes.Status200OK)
        {
            await loginProtectionService.RecordAttemptAsync(
                clientIp,
                string.Empty,
                true,
                userAgent,
                string.Empty,
                AdminLoginAttemptKind.LoginPage,
                path,
                context.RequestAborted);

            AdminSecurityLog.Information(
                "Login page viewed from {RemoteIP}",
                clientIp.Display);

            return;
        }

        if (HttpMethods.IsPost(method) &&
            path.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (context.Response.StatusCode is StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden)
        {
            await RecordAccessDenied(clientIp, userAgent, path, context.RequestAborted);
            return;
        }

        if (RedirectsToLogin(context.Response) && ShouldRecordProtectedAccess(context))
        {
            await RecordAccessDenied(clientIp, userAgent, path, context.RequestAborted);
        }
    }

    private async Task RecordAccessDenied(
        IpAddressPair clientIp,
        string userAgent,
        string path,
        CancellationToken cancellationToken)
    {
        string reason = $"Unauthenticated access to {path}";

        await loginProtectionService.RecordAttemptAsync(
            clientIp,
            string.Empty,
            false,
            userAgent,
            reason,
            AdminLoginAttemptKind.AccessDenied,
            path,
            cancellationToken);

        AdminSecurityLog.Warning(
            "Unauthenticated admin access from {RemoteIP} to {Path}",
            clientIp.Display,
            path);
    }

    private static bool ShouldRecordProtectedAccess(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            return context.Request.Headers.Accept.Any(value =>
                value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
        }

        return HttpMethods.IsPost(context.Request.Method) ||
               HttpMethods.IsPut(context.Request.Method) ||
               HttpMethods.IsPatch(context.Request.Method) ||
               HttpMethods.IsDelete(context.Request.Method);
    }

    private static bool RedirectsToLogin(HttpResponse response)
    {
        if (response.StatusCode is not (StatusCodes.Status302Found or StatusCodes.Status301MovedPermanently))
        {
            return false;
        }

        string location = response.Headers.Location.ToString();
        return location.Contains("/login", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIgnoredPath(string path) =>
        path.StartsWith("/iptv", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase);
}
