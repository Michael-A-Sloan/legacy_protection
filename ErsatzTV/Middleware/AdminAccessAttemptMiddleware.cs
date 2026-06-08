using ErsatzTV.Authorization;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

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
        if (AdminProtectionPaths.IsIgnoredPath(path))
        {
            return;
        }

        string method = context.Request.Method;
        string userAgent = context.Request.Headers.UserAgent.ToString();

        if (path.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
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
            cancellationToken: cancellationToken);

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

}
