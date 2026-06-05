using System.Net;
using System.Net.Sockets;

namespace ErsatzTV.Core.Networking;

public record IpAddressPair(string Ipv4, string Ipv6)
{
    public string Canonical => Ipv4 ?? Ipv6 ?? "unknown";

    public string Display => IpAddressFormatting.FormatDisplay(Ipv4, Ipv6);
}

public static class IpAddressFormatting
{
    public static IpAddressPair FromString(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "unknown")
        {
            return new IpAddressPair(null, null);
        }

        return IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed)
            ? FromAddress(parsed)
            : new IpAddressPair(ipAddress.Trim(), null);
    }

    public static IpAddressPair FromAddress(IPAddress address)
    {
        if (address is null)
        {
            return new IpAddressPair(null, null);
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return new IpAddressPair(address.ToString(), address.MapToIPv6().ToString());
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return new IpAddressPair(address.MapToIPv4().ToString(), address.ToString());
        }

        return new IpAddressPair(null, address.ToString());
    }

    public static string FormatDisplay(string ipv4, string ipv6)
    {
        if (!string.IsNullOrWhiteSpace(ipv4) && !string.IsNullOrWhiteSpace(ipv6))
        {
            return $"{ipv4} / {ipv6}";
        }

        return ipv4 ?? ipv6 ?? "unknown";
    }

    public static bool MatchesRule(string ruleIp, IpAddressPair client)
    {
        IpAddressPair rule = FromString(ruleIp);
        return Matches(client.Ipv4, rule.Ipv4) ||
               Matches(client.Ipv6, rule.Ipv6) ||
               Matches(client.Ipv4, rule.Ipv6) ||
               Matches(client.Ipv6, rule.Ipv4) ||
               Matches(client.Canonical, ruleIp);
    }

    private static bool Matches(string left, string right) =>
        !string.IsNullOrWhiteSpace(left) &&
        !string.IsNullOrWhiteSpace(right) &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
