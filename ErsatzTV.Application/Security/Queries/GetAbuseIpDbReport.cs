namespace ErsatzTV.Application.Security;

public record GetAbuseIpDbReport(string IpAddress) : IRequest<AbuseIpDbReportViewModel>;
