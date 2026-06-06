using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Infrastructure.Security;

public sealed class NullAnonymousIpDetectionService : IAnonymousIpDetectionService
{
    public bool IsConfigured => false;

    public string DatabasePath => string.Empty;

    public AnonymousIpLookupResult Lookup(string ipAddress) =>
        new(false, null, false, false, false, false);
}
