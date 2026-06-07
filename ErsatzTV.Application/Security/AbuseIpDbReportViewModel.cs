namespace ErsatzTV.Application.Security;

public class AbuseIpDbReportViewModel
{
    public bool Configured { get; set; }
    public bool LookupPerformed { get; set; }
    public string ErrorMessage { get; set; }
    public string QueriedIpAddress { get; set; }
    public int AbuseConfidenceScore { get; set; }
    public int MinScoreThreshold { get; set; }
    public bool WouldBlock { get; set; }
    public bool IsWhitelisted { get; set; }
    public bool IsPublic { get; set; }
    public int IpVersion { get; set; }
    public string CountryCode { get; set; }
    public string CountryName { get; set; }
    public string UsageType { get; set; }
    public string Isp { get; set; }
    public string Domain { get; set; }
    public int TotalReports { get; set; }
    public int NumDistinctUsers { get; set; }
    public DateTimeOffset? LastReportedAt { get; set; }
    public string AbuseIpDbUrl { get; set; }
}
