namespace ErsatzTV.Application.Security;

public class LoginIpSettingsViewModel
{
    public bool RateLimitEnabled { get; set; } = true;
    public int MaxFailedAttempts { get; set; } = 5;
    public int WindowSeconds { get; set; } = 300;
    public int LockoutSeconds { get; set; } = 900;
    public bool WhitelistEnabled { get; set; }
    public bool BlacklistEnabled { get; set; } = true;
}
