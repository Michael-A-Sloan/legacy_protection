using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class GetAdminIpRulesHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IAnonymousIpDetectionService anonymousIpDetectionService)
    : IRequestHandler<GetAdminIpRules, List<AdminIpRuleViewModel>>
{
    public async Task<List<AdminIpRuleViewModel>> Handle(
        GetAdminIpRules request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AdminIpRule> query = dbContext.AdminIpRules.AsNoTracking();

        if (request.RuleType.HasValue)
        {
            query = query.Where(r => r.RuleType == request.RuleType.Value);
        }

        List<AdminIpRule> rules = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        Dictionary<string, IpAttemptActivitySummary> activityByRule = request.RuleType == AdminIpRuleType.Blacklist
            ? await LoadBannedActivitySummaries(dbContext, rules, cancellationToken)
            : [];

        bool enrichVpnProxy = request.RuleType == AdminIpRuleType.Blacklist && AdminVpnBlockSettings.IsEnabled;

        return rules.Select(r =>
        {
            IpAddressPair pair = IpAddressFormatting.FromString(r.IpAddress);
            activityByRule.TryGetValue(r.IpAddress, out IpAttemptActivitySummary activity);

            bool isVpn = false;
            bool isProxy = false;
            bool isTor = false;
            if (enrichVpnProxy)
            {
                AnonymousIpLookupResult metadata =
                    AnonymousIpDetectionHelper.LookupClientIpMetadata(anonymousIpDetectionService, pair);
                isVpn = metadata.IsVpn;
                isProxy = metadata.IsProxy;
                isTor = metadata.IsTor;
            }

            return new AdminIpRuleViewModel
            {
                Id = r.Id,
                IpAddress = r.IpAddress,
                IpAddressV4 = pair.Ipv4 ?? string.Empty,
                IpAddressV6 = pair.Ipv6 ?? string.Empty,
                RuleType = r.RuleType,
                Note = r.Note,
                CreatedAt = r.CreatedAt,
                PageViewCount = activity?.PageViewCount ?? 0,
                LoginAttemptCount = activity?.LoginAttemptCount ?? 0,
                AccessDeniedCount = activity?.AccessDeniedCount ?? 0,
                TotalActivityCount = activity?.TotalActivityCount ?? 0,
                LastActivityAt = activity?.LastActivityAt,
                IsVpn = isVpn,
                IsProxy = isProxy,
                IsTor = isTor
            };
        }).ToList();
    }

    private static async Task<Dictionary<string, IpAttemptActivitySummary>> LoadBannedActivitySummaries(
        TvContext dbContext,
        List<AdminIpRule> blacklistRules,
        CancellationToken cancellationToken)
    {
        if (blacklistRules.Count == 0)
        {
            return [];
        }

        System.Collections.Generic.HashSet<string> allBannedAddresses =
            BannedIpAttemptMatching.ExpandRuleAddresses(blacklistRules.Select(r => r.IpAddress));

        List<AdminLoginAttempt> attempts = await dbContext.AdminLoginAttempts.AsNoTracking()
            .Where(a =>
                allBannedAddresses.Contains(a.IpAddress) ||
                allBannedAddresses.Contains(a.IpAddressV4) ||
                allBannedAddresses.Contains(a.IpAddressV6))
            .ToListAsync(cancellationToken);

        var summaries = blacklistRules.ToDictionary(
            r => r.IpAddress,
            r =>
            {
                IpAddressPair pair = IpAddressFormatting.FromString(r.IpAddress);
                return new IpAttemptActivitySummary
                {
                    IpAddress = r.IpAddress,
                    IpAddressV4 = pair.Ipv4 ?? string.Empty,
                    IpAddressV6 = pair.Ipv6 ?? string.Empty
                };
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (AdminLoginAttempt attempt in attempts)
        {
            AdminIpRule matchedRule = blacklistRules.FirstOrDefault(rule =>
                BannedIpAttemptMatching.MatchesAnyRuleAddress(
                    attempt,
                    BannedIpAttemptMatching.ExpandRuleAddresses([rule.IpAddress])));

            if (matchedRule is null)
            {
                continue;
            }

            IpAttemptActivityAggregator.ApplyAttemptToSummary(summaries[matchedRule.IpAddress], attempt);
        }

        return summaries;
    }
}
