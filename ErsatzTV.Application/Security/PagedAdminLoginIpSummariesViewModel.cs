namespace ErsatzTV.Application.Security;

public record PagedAdminLoginIpSummariesViewModel(int TotalCount, List<AdminLoginIpSummaryViewModel> Page);
