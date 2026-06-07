using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Core.Security;

public static class AbuseIpDbDetectionHelper
{
    public static bool IsClientIpBlocked(
        IAbuseIpDbDetectionService detectionService,
        IpAddressPair clientIp,
        int minScore,
        out AbuseIpDbLookupResult blockedLookup)
    {
        blockedLookup = AbuseIpDbLookupResult.NotConfigured();

        foreach (string candidate in new[] { clientIp.Ipv4, clientIp.Ipv6, clientIp.Canonical })
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidate == "unknown")
            {
                continue;
            }

            AbuseIpDbLookupResult lookup = detectionService.Lookup(candidate);
            if (lookup.LookupPerformed && AdminAbuseIpDbSettings.ShouldBlock(lookup, minScore))
            {
                blockedLookup = lookup;
                return true;
            }
        }

        return false;
    }

    public static string ResolveLookupIp(IpAddressPair clientIp)
    {
        foreach (string candidate in new[] { clientIp.Ipv4, clientIp.Ipv6, clientIp.Canonical })
        {
            if (!string.IsNullOrWhiteSpace(candidate) && candidate != "unknown")
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    public static string ResolveLookupIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return string.Empty;
        }

        IpAddressPair pair = IpAddressFormatting.FromString(ipAddress);
        return ResolveLookupIp(pair);
    }
}
