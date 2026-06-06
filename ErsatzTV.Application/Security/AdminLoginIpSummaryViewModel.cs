namespace ErsatzTV.Application.Security;

public class AdminLoginIpSummaryViewModel
{
    public string IpAddress { get; set; }
    public string IpAddressV4 { get; set; }
    public string IpAddressV6 { get; set; }
    public int PageViewCount { get; set; }
    public int LoginAttemptCount { get; set; }
    public int AccessDeniedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public int TotalActivityCount { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
