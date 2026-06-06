namespace ErsatzTV.Application.Security;

public class IpAttemptActivitySummary
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

    public AdminLoginIpSummaryViewModel ToViewModel() =>
        new()
        {
            IpAddress = IpAddress,
            IpAddressV4 = IpAddressV4,
            IpAddressV6 = IpAddressV6,
            PageViewCount = PageViewCount,
            LoginAttemptCount = LoginAttemptCount,
            AccessDeniedCount = AccessDeniedCount,
            SuccessCount = SuccessCount,
            FailedCount = FailedCount,
            TotalActivityCount = TotalActivityCount,
            LastActivityAt = LastActivityAt
        };
}
