using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class GetAbuseIpDbReportHandler(
    IAbuseIpDbDetectionService abuseIpDbDetectionService,
    IConfigElementRepository configElementRepository)
    : IRequestHandler<GetAbuseIpDbReport, AbuseIpDbReportViewModel>
{
    public async Task<AbuseIpDbReportViewModel> Handle(
        GetAbuseIpDbReport request,
        CancellationToken cancellationToken)
    {
        if (!AdminAbuseIpDbSettings.IsApiConfigured)
        {
            return new AbuseIpDbReportViewModel
            {
                Configured = false,
                ErrorMessage = "Set ETV_ABUSEIPDB_ENABLED=1 and ETV_ABUSEIPDB_API_KEY to enable AbuseIPDB lookups."
            };
        }

        string lookupIp = AbuseIpDbDetectionHelper.ResolveLookupIp(request.IpAddress);
        if (string.IsNullOrWhiteSpace(lookupIp))
        {
            return new AbuseIpDbReportViewModel
            {
                Configured = true,
                ErrorMessage = "No valid IP address was provided."
            };
        }

        int minScore = await AdminAbuseIpDbSettings.GetMinScoreAsync(configElementRepository, cancellationToken);
        AbuseIpDbLookupResult lookup = abuseIpDbDetectionService.Lookup(lookupIp);

        return new AbuseIpDbReportViewModel
        {
            Configured = true,
            LookupPerformed = lookup.LookupPerformed,
            ErrorMessage = lookup.ErrorMessage,
            QueriedIpAddress = lookup.IpAddress,
            AbuseConfidenceScore = lookup.AbuseConfidenceScore,
            MinScoreThreshold = minScore,
            WouldBlock = AdminAbuseIpDbSettings.ShouldBlock(lookup, minScore),
            IsWhitelisted = lookup.IsWhitelisted,
            IsPublic = lookup.IsPublic,
            IpVersion = lookup.IpVersion,
            CountryCode = lookup.CountryCode,
            CountryName = lookup.CountryName,
            UsageType = lookup.UsageType,
            Isp = lookup.Isp,
            Domain = lookup.Domain,
            TotalReports = lookup.TotalReports,
            NumDistinctUsers = lookup.NumDistinctUsers,
            LastReportedAt = lookup.LastReportedAt,
            AbuseIpDbUrl = $"https://www.abuseipdb.com/check/{Uri.EscapeDataString(lookupIp)}"
        };
    }
}
