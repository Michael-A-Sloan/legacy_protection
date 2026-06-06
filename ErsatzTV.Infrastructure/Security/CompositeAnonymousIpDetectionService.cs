using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Infrastructure.Security;

public sealed class CompositeAnonymousIpDetectionService(
    VpnApiAnonymousIpDetectionService vpnApiService,
    MaxMindAnonymousIpDetectionService maxMindService) : IAnonymousIpDetectionService
{
    public bool IsConfigured => vpnApiService.IsConfigured || maxMindService.IsConfigured;

    public string DatabasePath =>
        vpnApiService.IsConfigured ? vpnApiService.DatabasePath : maxMindService.DatabasePath;

    public AnonymousIpLookupResult Lookup(string ipAddress) =>
        vpnApiService.IsConfigured
            ? vpnApiService.Lookup(ipAddress)
            : maxMindService.Lookup(ipAddress);
}
