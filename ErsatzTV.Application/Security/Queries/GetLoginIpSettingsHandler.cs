using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class GetLoginIpSettingsHandler(IConfigElementRepository configElementRepository)
    : IRequestHandler<GetLoginIpSettings, LoginIpSettingsViewModel>
{
    public async Task<LoginIpSettingsViewModel> Handle(
        GetLoginIpSettings request,
        CancellationToken cancellationToken) =>
        new()
        {
            RateLimitEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpRateLimitEnabled, cancellationToken)
                .IfNoneAsync(true),
            MaxFailedAttempts = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpMaxFailedAttempts, cancellationToken)
                .IfNoneAsync(5),
            WindowSeconds = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpWindowSeconds, cancellationToken)
                .IfNoneAsync(300),
            LockoutSeconds = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpLockoutSeconds, cancellationToken)
                .IfNoneAsync(900),
            WhitelistEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpWhitelistEnabled, cancellationToken)
                .IfNoneAsync(false),
            BlacklistEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpBlacklistEnabled, cancellationToken)
                .IfNoneAsync(true),
            GeolocationRequiredFromEnvironment = AdminLoginGeolocationSettings.IsRequiredFromEnvironment,
            GeolocationRequired = AdminLoginGeolocationSettings.IsRequiredFromEnvironment ||
                                  await configElementRepository
                                      .GetValue<bool>(
                                          ConfigElementKey.AdminLoginGeolocationRequired,
                                          cancellationToken)
                                      .IfNoneAsync(false)
        };
}
