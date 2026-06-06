using ErsatzTV.Core;

namespace ErsatzTV.Core.Security;

public static class AdminVpnBlockSettings
{
    public static bool IsEnabled =>
        ParseBoolean(Environment.GetEnvironmentVariable("ETV_BLOCK_VPN")) ||
        ParseBoolean(Environment.GetEnvironmentVariable("ETV_VPN_BLOCK_ENABLED"));

    public static bool IsApiConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    public static string ApiKey
    {
        get
        {
            string key = Environment.GetEnvironmentVariable("ETV_VPNAPI_KEY");
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key.Trim();
            }

            key = Environment.GetEnvironmentVariable("ETV_VPNAPI_API_KEY");
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }

    public static string DatabasePath
    {
        get
        {
            string configured = Environment.GetEnvironmentVariable("ETV_ANONYMOUS_IP_DATABASE");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }

            string appData = Path.Combine(FileSystemLayout.AppDataFolder, "GeoLite2-Anonymous-IP.mmdb");
            if (File.Exists(appData))
            {
                return appData;
            }

            return Path.Combine(FileSystemLayout.AppDataFolder, "GeoIP2-Anonymous-IP.mmdb");
        }
    }

    public static bool ShouldBlock(bool vpn, bool proxy, bool tor, bool relay = false, bool anonymous = false)
    {
        if (ParseBoolean(Environment.GetEnvironmentVariable("ETV_BLOCK_VPN_ONLY")))
        {
            return vpn;
        }

        if (vpn || proxy || tor)
        {
            return true;
        }

        if (ParseBoolean(Environment.GetEnvironmentVariable("ETV_BLOCK_VPN_RELAY")) && relay)
        {
            return true;
        }

        return anonymous;
    }

    private static bool ParseBoolean(string value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}
