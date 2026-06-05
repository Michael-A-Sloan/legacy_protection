using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

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

        return Unit.Default;
    }
}
