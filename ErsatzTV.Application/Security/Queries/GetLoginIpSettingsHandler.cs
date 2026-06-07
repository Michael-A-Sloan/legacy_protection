using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class GetLoginIpSettingsHandler(
    IConfigElementRepository configElementRepository,
    IPublicBlocklistService publicBlocklistService)
    : IRequestHandler<GetLoginIpSettings, LoginIpSettingsViewModel>
{
    public async Task<LoginIpSettingsViewModel> Handle(
        GetLoginIpSettings request,
        CancellationToken cancellationToken)
    {
        PublicBlocklistSettings blocklistSettings =
            await PublicBlocklistSettings.LoadAsync(configElementRepository, cancellationToken);

        return new LoginIpSettingsViewModel
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
                                      .IfNoneAsync(false),
            AbuseIpDbAvailable = AdminAbuseIpDbSettings.IsFeatureAvailable,
            AbuseIpDbBlockEnabled = AdminAbuseIpDbSettings.IsFeatureAvailable &&
                                    await configElementRepository
                                        .GetValue<bool>(
                                            ConfigElementKey.AdminLoginIpAbuseIpDbEnabled,
                                            cancellationToken)
                                        .IfNoneAsync(true),
            AbuseIpDbMinScore = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpAbuseIpDbMinScore, cancellationToken)
                .IfNoneAsync(AdminAbuseIpDbSettings.DefaultMinScore),
            PublicBlocklistsMasterEnabled = blocklistSettings.MasterEnabled,
            PublicBlocklists = BuildPublicBlocklistViewModels(blocklistSettings)
        };
    }

    private List<PublicBlocklistItemViewModel> BuildPublicBlocklistViewModels(
        PublicBlocklistSettings blocklistSettings)
    {
        IReadOnlyList<PublicBlocklistStatus> statuses = publicBlocklistService.GetStatuses();
        IReadOnlyList<PublicBlocklistDefinition> definitions = blocklistSettings.GetAllDefinitions();

        return definitions.Select(definition =>
        {
            PublicBlocklistStatus status =
                statuses.FirstOrDefault(s => s.Id == definition.Id) ??
                new PublicBlocklistStatus(
                    definition.Id,
                    definition.Name,
                    definition.Recommended,
                    blocklistSettings.IsListEnabled(definition),
                    0,
                    0,
                    null,
                    null,
                    string.Empty,
                    false);

            return new PublicBlocklistItemViewModel
            {
                Id = definition.Id,
                Name = definition.Name,
                Category = definition.Category,
                Description = definition.Description,
                SourceLabel = definition.SourceLabel,
                SourceUrl = definition.SourceUrl,
                Format = definition.Format,
                UpdateIntervalHours = (int)definition.UpdateInterval.TotalHours,
                Recommended = definition.Recommended,
                IsCustom = definition.IsCustom,
                Enabled = blocklistSettings.IsListEnabled(definition),
                EntryCount = status.EntryCount,
                NetworkCount = status.NetworkCount,
                LastUpdatedUtc = status.LastUpdatedUtc,
                NextUpdateUtc = status.NextUpdateUtc,
                LastError = status.LastError,
                IsUpdating = status.IsUpdating
            };
        }).ToList();
    }
}
