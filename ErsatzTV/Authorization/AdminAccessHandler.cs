using System.Net;
using ErsatzTV.Core.Networking;
using Microsoft.AspNetCore.Authorization;

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

        if (context.Resource is HttpContext httpContext &&
            LocalIpExemption.IsExempt(httpContext.Connection.RemoteIpAddress))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
