using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public static class BannedIpAttemptMatching
{
    public static async Task<List<string>> GetBlacklistRuleAddresses(
        TvContext dbContext,
        CancellationToken cancellationToken) =>
        await dbContext.AdminIpRules.AsNoTracking()
            .Where(r => r.RuleType == AdminIpRuleType.Blacklist)
            .Select(r => r.IpAddress)
            .ToListAsync(cancellationToken);

    public static System.Collections.Generic.HashSet<string> ExpandRuleAddresses(IEnumerable<string> ruleAddresses)
    {
        var addresses = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string ruleAddress in ruleAddresses)
        {
            foreach (string expanded in ExpandRuleAddresses(ruleAddress))
            {
                addresses.Add(expanded);
            }
        }

        return addresses;
    }

    public static IEnumerable<string> ExpandRuleAddresses(string ruleAddress)
    {
        if (string.IsNullOrWhiteSpace(ruleAddress))
        {
            yield break;
        }

        yield return ruleAddress.Trim();

        IpAddressPair pair = IpAddressFormatting.FromString(ruleAddress);

        if (!string.IsNullOrWhiteSpace(pair.Ipv4))
        {
            yield return pair.Ipv4;
        }

        if (!string.IsNullOrWhiteSpace(pair.Ipv6))
        {
            yield return pair.Ipv6;
        }

        if (!string.IsNullOrWhiteSpace(pair.Canonical) && pair.Canonical != "unknown")
        {
            yield return pair.Canonical;
        }
    }

    public static IQueryable<AdminLoginAttempt> ApplyScope(
        IQueryable<AdminLoginAttempt> query,
        LoginAttemptScope scope,
        System.Collections.Generic.HashSet<string> bannedAddresses)
    {
        if (bannedAddresses.Count == 0)
        {
            return scope == LoginAttemptScope.Banned
                ? query.Where(_ => false)
                : query;
        }

        return scope == LoginAttemptScope.Banned
            ? query.Where(a =>
                bannedAddresses.Contains(a.IpAddress) ||
                bannedAddresses.Contains(a.IpAddressV4) ||
                bannedAddresses.Contains(a.IpAddressV6))
            : query.Where(a =>
                !bannedAddresses.Contains(a.IpAddress) &&
                !bannedAddresses.Contains(a.IpAddressV4) &&
                !bannedAddresses.Contains(a.IpAddressV6));
    }

    public static IQueryable<AdminLoginAttempt> ApplyBannedIpFilter(
        IQueryable<AdminLoginAttempt> query,
        string bannedIpFilter)
    {
        if (string.IsNullOrWhiteSpace(bannedIpFilter))
        {
            return query;
        }

        System.Collections.Generic.HashSet<string> addresses = ExpandRuleAddresses([bannedIpFilter]);

        return query.Where(a =>
            addresses.Contains(a.IpAddress) ||
            addresses.Contains(a.IpAddressV4) ||
            addresses.Contains(a.IpAddressV6));
    }

    public static bool MatchesAnyRuleAddress(
        AdminLoginAttempt attempt,
        System.Collections.Generic.HashSet<string> ruleAddresses)
    {
        if (ruleAddresses.Count == 0)
        {
            return false;
        }

        return ruleAddresses.Contains(attempt.IpAddress) ||
               ruleAddresses.Contains(attempt.IpAddressV4) ||
               ruleAddresses.Contains(attempt.IpAddressV6);
    }
}
