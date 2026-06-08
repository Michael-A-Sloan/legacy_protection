using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Core.Security;

public static class AnonymousIpDetectionHelper
{
    public static AnonymousIpLookupResult LookupClientIp(
        IAnonymousIpDetectionService detectionService,
        IpAddressPair clientIp)
    {
        foreach (string candidate in new[] { clientIp.Ipv4, clientIp.Ipv6, clientIp.Canonical })
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidate == "unknown")
            {
                continue;
            }

            AnonymousIpLookupResult lookup = detectionService.Lookup(candidate);
            if (lookup.IsBlocked)
            {
                return lookup;
            }
        }

        return new AnonymousIpLookupResult(false, null, false, false, false, false);
    }

    public static AnonymousIpLookupResult LookupClientIpMetadata(
        IAnonymousIpDetectionService detectionService,
        IpAddressPair clientIp)
    {
        AnonymousIpLookupResult lastPerformed = new(false, null, false, false, false, false);

        foreach (string candidate in new[] { clientIp.Ipv4, clientIp.Ipv6, clientIp.Canonical })
        {
            if (string.IsNullOrWhiteSpace(candidate) || candidate == "unknown")
            {
                continue;
            }

            AnonymousIpLookupResult lookup = detectionService.Lookup(candidate);
            if (!lookup.LookupPerformed)
            {
                continue;
            }

            lastPerformed = lookup;
            if (lookup.IsVpn || lookup.IsProxy || lookup.IsTor)
            {
                return lookup;
            }
        }

        return lastPerformed;
    }
}
