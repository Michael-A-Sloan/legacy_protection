using System.Text.Json;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Security;

public sealed class LoginIpAutoBanSettings
{
    public bool ThreatIntelEnabled { get; set; } = true;

    public bool ActivityEnabled { get; set; }

    public int ActivityMinFailedAttempts { get; set; } = 5;

    public int ActivityWindowDays { get; set; } = 30;

    public bool ActivityIncludeAccessDenied { get; set; } = true;

    public DateTime? LastScanUtc { get; set; }

    public int LastScanScannedCount { get; set; }

    public int LastScanBannedCount { get; set; }

    public int LastScanSkippedCount { get; set; }

    public static async Task<LoginIpAutoBanSettings> LoadAsync(
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken)
    {
        Option<string> stored = await configElementRepository
            .GetValue<string>(ConfigElementKey.AdminLoginIpAutoBan, cancellationToken);

        return stored.Match(
            json =>
            {
                try
                {
                    return JsonSerializer.Deserialize<LoginIpAutoBanSettings>(json) ?? CreateDefaults();
                }
                catch (JsonException)
                {
                    return CreateDefaults();
                }
            },
            CreateDefaults);
    }

    public static async Task SaveAsync(
        IConfigElementRepository configElementRepository,
        LoginIpAutoBanSettings settings,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(settings);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpAutoBan,
            json,
            cancellationToken);
    }

    public bool IsAnyScanEnabled => ThreatIntelEnabled || ActivityEnabled;

    private static LoginIpAutoBanSettings CreateDefaults() => new();
}
