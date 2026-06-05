using System.Net;
using ErsatzTV.Core.Networking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ErsatzTV.Authorization;

public class AdminAccessHandler : AuthorizationHandler<AdminAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        HttpContext httpContext = ResolveHttpContext(context);
        if (httpContext is not null &&
            LocalIpExemption.IsExempt(httpContext.Connection.RemoteIpAddress))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static HttpContext ResolveHttpContext(AuthorizationHandlerContext context) =>
        context.Resource switch
        {
            HttpContext httpContext => httpContext,
            AuthorizationFilterContext mvcContext => mvcContext.HttpContext,
            _ => null
        };
}
