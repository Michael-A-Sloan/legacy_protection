namespace ErsatzTV.Application.Security;

public class LoginIpSettingsViewModel
{
    public bool RateLimitEnabled { get; set; } = true;
    public int MaxFailedAttempts { get; set; } = 5;
    public int WindowSeconds { get; set; } = 300;
    public int LockoutSeconds { get; set; } = 900;
    public bool WhitelistEnabled { get; set; }
    public bool BlacklistEnabled { get; set; } = true;
    public bool GeolocationRequired { get; set; }
    public bool GeolocationRequiredFromEnvironment { get; set; }
    public bool AbuseIpDbAvailable { get; set; }
    public bool AbuseIpDbBlockEnabled { get; set; } = true;
    public int AbuseIpDbMinScore { get; set; } = 75;
    public bool PublicBlocklistsMasterEnabled { get; set; } = true;
    public List<PublicBlocklistItemViewModel> PublicBlocklists { get; set; } = [];
    public bool AutoBanThreatIntelEnabled { get; set; } = true;
    public bool AutoBanActivityEnabled { get; set; }
    public int AutoBanActivityMinFailedAttempts { get; set; } = 5;
    public int AutoBanActivityWindowDays { get; set; } = 30;
    public bool AutoBanActivityIncludeAccessDenied { get; set; } = true;
    public DateTime? AutoBanLastScanUtc { get; set; }
    public int AutoBanLastScanScannedCount { get; set; }
    public int AutoBanLastScanBannedCount { get; set; }
    public int AutoBanLastScanSkippedCount { get; set; }
    public bool VpnBlockEnabled { get; set; }
    public bool ShowVpnProxyBannedIps { get; set; }
}
