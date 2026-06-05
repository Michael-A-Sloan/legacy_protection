namespace ErsatzTV.Application.Security;

public class PagedAdminLoginAttemptsViewModel(int totalCount, List<AdminLoginAttemptViewModel> page)
{
    public int TotalCount { get; } = totalCount;
    public List<AdminLoginAttemptViewModel> Page { get; } = page;
}
