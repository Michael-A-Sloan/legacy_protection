using System.Net;
using ErsatzTV.Core.Networking;
using Microsoft.AspNetCore.Http;

namespace ErsatzTV;

public static class ClientIpHelper
{
    public static IpAddressPair GetClientIpInfo(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            !string.IsNullOrWhiteSpace(forwardedFor))
        {
            string firstHop = forwardedFor.ToString().Split(',')[0].Trim();
            IpAddressPair forwarded = IpAddressFormatting.FromString(firstHop);
            if (!string.IsNullOrWhiteSpace(forwarded.Canonical) && forwarded.Canonical != "unknown")
            {
                return forwarded;
            }
        }

        return IpAddressFormatting.FromAddress(httpContext.Connection.RemoteIpAddress);
    }

    public static string GetClientIp(HttpContext httpContext) =>
        GetClientIpInfo(httpContext).Canonical;

    public static string NormalizeIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return string.Empty;
        }

        if (IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed))
        {
            return parsed.ToString();
        }

        return ipAddress.Trim();
    }

    public static bool IsExemptLocalAddress(string ipAddress) => LocalIpExemption.IsExempt(ipAddress);
}
