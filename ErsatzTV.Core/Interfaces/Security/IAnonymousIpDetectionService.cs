namespace ErsatzTV.Core.Interfaces.Security;

public record AnonymousIpLookupResult(
    bool IsBlocked,
    string DenyReason,
    bool LookupPerformed,
    bool IsVpn,
    bool IsProxy,
    bool IsTor);

public interface IAnonymousIpDetectionService
{
    bool IsConfigured { get; }

    string DatabasePath { get; }

    AnonymousIpLookupResult Lookup(string ipAddress);
}
