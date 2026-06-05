using System.Net;

namespace ErsatzTV.Core.Networking;

public static class LocalIpExemption
{
    public static bool IsExempt(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed) && IsExempt(parsed);
    }

    public static bool IsExempt(IPAddress address)
    {
        if (address is null)
        {
            return false;
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        // Normalize IPv4-mapped IPv6 loopback (e.g. ::ffff:127.0.0.1)
        if (address.IsIPv4MappedToIPv6)
        {
            return IPAddress.IsLoopback(address.MapToIPv4());
        }

        return false;
    }
}
