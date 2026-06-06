namespace ErsatzTV.Application.Security;

public record GetAdminLoginIpSummaries(
    int PageNum,
    int PageSize,
    string Search,
    string SortLabel = "lastactivity",
    bool SortDescending = true,
    LoginAttemptScope Scope = LoginAttemptScope.Active) : IRequest<PagedAdminLoginIpSummariesViewModel>;
