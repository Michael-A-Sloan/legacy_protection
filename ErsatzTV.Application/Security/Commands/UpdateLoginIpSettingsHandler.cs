using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class UpdateLoginIpSettingsHandler(IConfigElementRepository configElementRepository)
    : IRequestHandler<UpdateLoginIpSettings, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        UpdateLoginIpSettings request,
        CancellationToken cancellationToken)
    {
        LoginIpSettingsViewModel settings = request.Settings;

        if (settings.MaxFailedAttempts < 1)
        {
            return BaseError.New("Max failed attempts must be at least 1.");
        }

        if (settings.WindowSeconds < 60)
        {
            return BaseError.New("Rate limit window must be at least 60 seconds.");
        }

        if (settings.LockoutSeconds < 60)
        {
            return BaseError.New("Lockout duration must be at least 60 seconds.");
        }

        if (settings.AbuseIpDbMinScore is < 0 or > 100)
        {
            return BaseError.New("AbuseIPDB minimum score must be between 0 and 100.");
        }

        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpRateLimitEnabled,
            settings.RateLimitEnabled,
            cancellationToken);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpMaxFailedAttempts,
            settings.MaxFailedAttempts,
            cancellationToken);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpWindowSeconds,
            settings.WindowSeconds,
            cancellationToken);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpLockoutSeconds,
            settings.LockoutSeconds,
            cancellationToken);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpWhitelistEnabled,
            settings.WhitelistEnabled,
            cancellationToken);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpBlacklistEnabled,
            settings.BlacklistEnabled,
            cancellationToken);

        if (!AdminLoginGeolocationSettings.IsRequiredFromEnvironment)
        {
            await configElementRepository.Upsert(
                ConfigElementKey.AdminLoginGeolocationRequired,
                settings.GeolocationRequired,
                cancellationToken);
        }

        if (AdminAbuseIpDbSettings.IsFeatureAvailable)
        {
            await configElementRepository.Upsert(
                ConfigElementKey.AdminLoginIpAbuseIpDbEnabled,
                settings.AbuseIpDbBlockEnabled,
                cancellationToken);
            await configElementRepository.Upsert(
                ConfigElementKey.AdminLoginIpAbuseIpDbMinScore,
                settings.AbuseIpDbMinScore,
                cancellationToken);
        }

        return Unit.Default;
    }
}
