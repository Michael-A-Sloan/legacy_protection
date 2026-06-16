using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class UpdateLoginIpSettingsHandler(
    IConfigElementRepository configElementRepository,
    IPublicBlocklistService publicBlocklistService)
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

        if (settings.AutoBanActivityMinFailedAttempts < 1)
        {
            return BaseError.New("Auto-ban activity minimum failed attempts must be at least 1.");
        }

        if (settings.AutoBanActivityWindowDays is < 1 or > 365)
        {
            return BaseError.New("Auto-ban activity window must be between 1 and 365 days.");
        }

        if (settings.AutoBanVpnEnabled && !AdminVpnBlockSettings.IsDetectionAvailable)
        {
            return BaseError.New(
                "VPN/proxy auto-ban requires VPNAPI or a MaxMind Anonymous IP database to be configured.");
        }

        List<CustomPublicBlocklistEntry> customLists = settings.PublicBlocklists
            .Where(item => item.IsCustom)
            .Select(item => new CustomPublicBlocklistEntry
            {
                Id = item.Id,
                Name = item.Name,
                SourceUrl = item.SourceUrl,
                Format = item.Format,
                UpdateIntervalHours = item.UpdateIntervalHours
            })
            .ToList();

        Option<BaseError> customValidation = PublicBlocklistValidation.ValidateCustomEntries(customLists);
        foreach (BaseError error in customValidation)
        {
            return error;
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

        var blocklistSettings = new PublicBlocklistSettings
        {
            MasterEnabled = settings.PublicBlocklistsMasterEnabled,
            EnabledById = settings.PublicBlocklists.ToDictionary(
                item => item.Id,
                item => item.Enabled,
                StringComparer.OrdinalIgnoreCase),
            CustomLists = customLists
        };

        await PublicBlocklistSettings.SaveAsync(configElementRepository, blocklistSettings, cancellationToken);

        LoginIpAutoBanSettings existingAutoBanSettings =
            await LoginIpAutoBanSettings.LoadAsync(configElementRepository, cancellationToken);

        var autoBanSettings = new LoginIpAutoBanSettings
        {
            ThreatIntelEnabled = settings.AutoBanThreatIntelEnabled,
            ActivityEnabled = settings.AutoBanActivityEnabled,
            ActivityMinFailedAttempts = settings.AutoBanActivityMinFailedAttempts,
            ActivityWindowDays = settings.AutoBanActivityWindowDays,
            ActivityIncludeAccessDenied = settings.AutoBanActivityIncludeAccessDenied,
            VpnEnabled = settings.AutoBanVpnEnabled,
            LastScanUtc = existingAutoBanSettings.LastScanUtc,
            LastScanScannedCount = existingAutoBanSettings.LastScanScannedCount,
            LastScanBannedCount = existingAutoBanSettings.LastScanBannedCount,
            LastScanSkippedCount = existingAutoBanSettings.LastScanSkippedCount
        };

        await LoginIpAutoBanSettings.SaveAsync(configElementRepository, autoBanSettings, cancellationToken);

        if (AdminVpnBlockSettings.IsDetectionAvailable)
        {
            await configElementRepository.Upsert(
                ConfigElementKey.AdminLoginIpShowVpnProxyBannedIps,
                settings.ShowVpnProxyBannedIps,
                cancellationToken);
        }

        foreach (CustomPublicBlocklistEntry custom in customLists)
        {
            await publicBlocklistService.RefreshListAsync(custom.Id, cancellationToken);
        }

        return Unit.Default;
    }
}
