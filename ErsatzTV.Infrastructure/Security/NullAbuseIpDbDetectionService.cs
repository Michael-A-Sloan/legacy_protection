using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Infrastructure.Security;

public sealed class NullAbuseIpDbDetectionService : IAbuseIpDbDetectionService
{
    public bool IsConfigured => false;

    public AbuseIpDbLookupResult Lookup(string ipAddress) => AbuseIpDbLookupResult.NotConfigured();
}
