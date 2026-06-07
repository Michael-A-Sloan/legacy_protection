using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;

namespace ErsatzTV.Core.Security;

public static class AdminAbuseIpDbSettings
{
    private const int DefaultMinScoreValue = 75;
    private const int DefaultMaxAgeDaysValue = 90;

    public static bool IsEnabledFromEnvironment =>
        ParseBoolean(Environment.GetEnvironmentVariable("ETV_ABUSEIPDB_ENABLED"));

    public static bool IsApiConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static bool IsFeatureAvailable => IsEnabledFromEnvironment && IsApiConfigured;

    public static string ApiKey
    {
        get
        {
            string key = Environment.GetEnvironmentVariable("ETV_ABUSEIPDB_API_KEY");
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }

    public static int DefaultMinScore => ParseInt(Environment.GetEnvironmentVariable("ETV_ABUSEIPDB_MIN_SCORE"), DefaultMinScoreValue, 0, 100);

    public static int MaxAgeInDays => ParseInt(Environment.GetEnvironmentVariable("ETV_ABUSEIPDB_MAX_AGE_DAYS"), DefaultMaxAgeDaysValue, 1, 365);

    public static async Task<bool> IsBlockingEnabledAsync(
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken)
    {
        if (!IsFeatureAvailable)
        {
            return false;
        }

        return await configElementRepository
            .GetValue<bool>(ConfigElementKey.AdminLoginIpAbuseIpDbEnabled, cancellationToken)
            .IfNoneAsync(true);
    }

    public static async Task<int> GetMinScoreAsync(
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken)
    {
        int configured = await configElementRepository
            .GetValue<int>(ConfigElementKey.AdminLoginIpAbuseIpDbMinScore, cancellationToken)
            .IfNoneAsync(DefaultMinScore);

        return Math.Clamp(configured, 0, 100);
    }

    public static bool ShouldBlock(AbuseIpDbLookupResult lookup, int minScore)
    {
        if (!lookup.LookupPerformed || lookup.IsWhitelisted)
        {
            return false;
        }

        return lookup.AbuseConfidenceScore >= minScore;
    }

    public static string BuildDenyReason(AbuseIpDbLookupResult lookup) =>
        $"IP address has a high abuse confidence score ({lookup.AbuseConfidenceScore}%).";

    private static bool ParseBoolean(string value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static int ParseInt(string value, int defaultValue, int min, int max)
    {
        if (!int.TryParse(value, out int parsed))
        {
            return defaultValue;
        }

        return Math.Clamp(parsed, min, max);
    }
}
