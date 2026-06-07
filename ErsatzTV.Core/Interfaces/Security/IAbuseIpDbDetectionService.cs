namespace ErsatzTV.Core.Interfaces.Security;

public record AbuseIpDbLookupResult(
    bool LookupPerformed,
    string ErrorMessage,
    string IpAddress,
    int AbuseConfidenceScore,
    bool IsWhitelisted,
    bool IsPublic,
    int IpVersion,
    string CountryCode,
    string CountryName,
    string UsageType,
    string Isp,
    string Domain,
    int TotalReports,
    int NumDistinctUsers,
    DateTimeOffset? LastReportedAt)
{
    public static AbuseIpDbLookupResult NotConfigured() =>
        new(false, "AbuseIPDB is not configured.", string.Empty, 0, false, false, 0, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, 0, 0, null);

    public static AbuseIpDbLookupResult Failed(string ipAddress, string message) =>
        new(false, message, ipAddress, 0, false, false, 0, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, 0, 0, null);
}

public interface IAbuseIpDbDetectionService
{
    bool IsConfigured { get; }

    AbuseIpDbLookupResult Lookup(string ipAddress);
}
