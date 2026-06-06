using System.Collections;

namespace ErsatzTV.Core;

public static class TroubleshootingEnvironmentSanitizer
{
    private const string RedactedValue = "(redacted)";

    private static readonly System.Collections.Generic.HashSet<string> FullyRedactedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "ETV_ADMIN_PASSWORD",
        "ETV_ADMIN_USERNAME",
        "ETV_VPNAPI_KEY",
        "ETV_VPNAPI_API_KEY"
    };

    public static Dictionary<string, string> CollectSanitizedEnvironmentVariables()
    {
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            if (de is not { Key: string key, Value: string value })
            {
                continue;
            }

            if (!ShouldInclude(key))
            {
                continue;
            }

            environment[key] = Sanitize(key, value);
        }

        return environment;
    }

    public static string Sanitize(string key, string value)
    {
        if (FullyRedactedKeys.Contains(key) || IsSensitiveKey(key))
        {
            return string.IsNullOrWhiteSpace(value) ? value : RedactedValue;
        }

        return value;
    }

    private static bool ShouldInclude(string key) =>
        key.StartsWith("ETV_", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("ASPNETCORE_", StringComparison.OrdinalIgnoreCase)
        || key.Equals("PROVIDER", StringComparison.OrdinalIgnoreCase)
        || key.StartsWith("ELASTICSEARCH", StringComparison.OrdinalIgnoreCase);

    private static bool IsSensitiveKey(string key) =>
        key.Contains("PASSWORD", StringComparison.OrdinalIgnoreCase)
        || key.Contains("SECRET", StringComparison.OrdinalIgnoreCase)
        || key.Contains("TOKEN", StringComparison.OrdinalIgnoreCase)
        || key.Contains("SIGNING", StringComparison.OrdinalIgnoreCase)
        || key.Contains("API_KEY", StringComparison.OrdinalIgnoreCase)
        || key.Contains("PRIVATE", StringComparison.OrdinalIgnoreCase);
}
