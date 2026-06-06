using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class GetAdminLoginIpSummariesHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAdminLoginIpSummaries, PagedAdminLoginIpSummariesViewModel>
{
    public async Task<PagedAdminLoginIpSummariesViewModel> Handle(
        GetAdminLoginIpSummaries request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> blacklistRules =
            await BannedIpAttemptMatching.GetBlacklistRuleAddresses(dbContext, cancellationToken);
        System.Collections.Generic.HashSet<string> bannedAddresses =
            BannedIpAttemptMatching.ExpandRuleAddresses(blacklistRules);

        IQueryable<AdminLoginAttempt> query = dbContext.AdminLoginAttempts.AsNoTracking();
        query = BannedIpAttemptMatching.ApplyScope(query, request.Scope, bannedAddresses);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();
            query = query.Where(a =>
                a.IpAddress.Contains(search) ||
                a.IpAddressV4.Contains(search) ||
                a.IpAddressV6.Contains(search) ||
                a.Username.Contains(search) ||
                a.DenyReason.Contains(search) ||
                a.UserAgent.Contains(search) ||
                a.RequestPath.Contains(search));
        }

        List<AdminLoginAttempt> attempts = await query.ToListAsync(cancellationToken);
        List<AdminLoginIpSummaryViewModel> summaries = IpAttemptActivityAggregator.Aggregate(attempts)
            .Values
            .Select(summary => summary.ToViewModel())
            .ToList();

        summaries = ApplySort(summaries, request.SortLabel, request.SortDescending);

        int totalCount = summaries.Count;
        List<AdminLoginIpSummaryViewModel> page = summaries
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedAdminLoginIpSummariesViewModel(totalCount, page);
    }

    private static List<AdminLoginIpSummaryViewModel> ApplySort(
        List<AdminLoginIpSummaryViewModel> summaries,
        string sortLabel,
        bool descending) =>
        sortLabel?.ToLowerInvariant() switch
        {
            "ipaddress" or "ipaddressv4" => descending
                ? summaries.OrderByDescending(s => s.IpAddressV4).ThenByDescending(s => s.IpAddress).ToList()
                : summaries.OrderBy(s => s.IpAddressV4).ThenBy(s => s.IpAddress).ToList(),
            "ipaddressv6" => descending
                ? summaries.OrderByDescending(s => s.IpAddressV6).ThenByDescending(s => s.IpAddress).ToList()
                : summaries.OrderBy(s => s.IpAddressV6).ThenBy(s => s.IpAddress).ToList(),
            "pageviewcount" or "views" => descending
                ? summaries.OrderByDescending(s => s.PageViewCount).ToList()
                : summaries.OrderBy(s => s.PageViewCount).ToList(),
            "loginattemptcount" or "logins" => descending
                ? summaries.OrderByDescending(s => s.LoginAttemptCount).ToList()
                : summaries.OrderBy(s => s.LoginAttemptCount).ToList(),
            "accessdeniedcount" or "access" => descending
                ? summaries.OrderByDescending(s => s.AccessDeniedCount).ToList()
                : summaries.OrderBy(s => s.AccessDeniedCount).ToList(),
            "successcount" or "success" => descending
                ? summaries.OrderByDescending(s => s.SuccessCount).ToList()
                : summaries.OrderBy(s => s.SuccessCount).ToList(),
            "failedcount" or "failed" => descending
                ? summaries.OrderByDescending(s => s.FailedCount).ToList()
                : summaries.OrderBy(s => s.FailedCount).ToList(),
            "totalactivitycount" or "total" => descending
                ? summaries.OrderByDescending(s => s.TotalActivityCount).ToList()
                : summaries.OrderBy(s => s.TotalActivityCount).ToList(),
            _ => descending
                ? summaries.OrderByDescending(s => s.LastActivityAt).ToList()
                : summaries.OrderBy(s => s.LastActivityAt).ToList()
        };
}
