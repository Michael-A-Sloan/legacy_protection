using ErsatzTV.Core.Security;

namespace ErsatzTV.Middleware;

public sealed class AntiDiscoveryMiddleware(RequestDelegate next)
{
    private const string RobotsTagValue = "noindex, nofollow, noarchive, nosnippet, noimageindex, notranslate";
    private const string ReferrerPolicyValue = "no-referrer";
    private const string PermissionsPolicyValue = "interest-cohort=()";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!AdminProtectionHelper.IsEnabled)
        {
            await next(context);
            return;
        }

        string path = context.Request.Path.Value ?? "/";

        if (AdminProtectionPaths.IsBlockedDiscoveryPath(path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (AdminProtectionPaths.ShouldApplyPrivacyHeaders(path))
        {
            context.Response.OnStarting(() =>
            {
                IHeaderDictionary headers = context.Response.Headers;
                headers["X-Robots-Tag"] = RobotsTagValue;
                headers["Referrer-Policy"] = ReferrerPolicyValue;
                headers["Permissions-Policy"] = PermissionsPolicyValue;
                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
