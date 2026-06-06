using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Core.Interfaces.Security;

public record AdminLoginAccessResult(bool Allowed, string DenyReason);

public interface IAdminLoginProtectionService
{
    Task<bool> IsIpBannedAsync(IpAddressPair clientIp, CancellationToken cancellationToken);

    Task<AdminLoginAccessResult> CheckAccessAsync(IpAddressPair clientIp, CancellationToken cancellationToken);

    Task RecordAttemptAsync(
        IpAddressPair clientIp,
        string username,
        bool success,
        string userAgent,
        string denyReason,
        AdminLoginAttemptKind attemptKind = AdminLoginAttemptKind.Login,
        string requestPath = "",
        double? latitude = null,
        double? longitude = null,
        double? locationAccuracyMeters = null,
        CancellationToken cancellationToken = default);
}
