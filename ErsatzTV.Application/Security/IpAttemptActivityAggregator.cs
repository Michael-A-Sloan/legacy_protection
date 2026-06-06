using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;

namespace ErsatzTV.Application.Security;

public static class IpAttemptActivityAggregator
{
    public static Dictionary<string, IpAttemptActivitySummary> Aggregate(
        IEnumerable<AdminLoginAttempt> attempts)
    {
        var summaries = new Dictionary<string, IpAttemptActivitySummary>(StringComparer.OrdinalIgnoreCase);

        foreach (AdminLoginAttempt attempt in attempts)
        {
            AddAttempt(summaries, attempt);
        }

        return summaries;
    }

    public static void AddAttempt(
        Dictionary<string, IpAttemptActivitySummary> summaries,
        AdminLoginAttempt attempt)
    {
        string key = FindMatchingKey(summaries, attempt) ?? CreateKey(attempt);
        if (!summaries.TryGetValue(key, out IpAttemptActivitySummary summary))
        {
            summary = CreateSummary(attempt, key);
            summaries[key] = summary;
        }

        ApplyAttemptToSummary(summary, attempt);
    }

    public static void ApplyAttemptToSummary(IpAttemptActivitySummary summary, AdminLoginAttempt attempt)
    {
        MergeAddresses(summary, attempt);
        summary.TotalActivityCount++;

        if (!summary.LastActivityAt.HasValue || attempt.Timestamp > summary.LastActivityAt)
        {
            summary.LastActivityAt = attempt.Timestamp;
        }

        switch (attempt.AttemptKind)
        {
            case AdminLoginAttemptKind.LoginPage:
                summary.PageViewCount++;
                break;
            case AdminLoginAttemptKind.Login:
                summary.LoginAttemptCount++;
                if (attempt.Success)
                {
                    summary.SuccessCount++;
                }
                else
                {
                    summary.FailedCount++;
                }

                break;
            case AdminLoginAttemptKind.AccessDenied:
                summary.AccessDeniedCount++;
                summary.FailedCount++;
                break;
        }
    }

    private static string FindMatchingKey(
        Dictionary<string, IpAttemptActivitySummary> summaries,
        AdminLoginAttempt attempt)
    {
        foreach (string key in summaries.Keys)
        {
            if (BannedIpAttemptMatching.MatchesAnyRuleAddress(
                    attempt,
                    BannedIpAttemptMatching.ExpandRuleAddresses([key])))
            {
                return key;
            }
        }

        return null;
    }

    private static string CreateKey(AdminLoginAttempt attempt)
    {
        if (!string.IsNullOrWhiteSpace(attempt.IpAddress) &&
            !string.Equals(attempt.IpAddress, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return attempt.IpAddress.Trim();
        }

        IpAddressPair pair = GetAttemptPair(attempt);
        return pair.Canonical;
    }

    private static IpAttemptActivitySummary CreateSummary(AdminLoginAttempt attempt, string key)
    {
        IpAddressPair pair = GetAttemptPair(attempt);
        return new IpAttemptActivitySummary
        {
            IpAddress = key,
            IpAddressV4 = pair.Ipv4 ?? string.Empty,
            IpAddressV6 = pair.Ipv6 ?? string.Empty
        };
    }

    private static void MergeAddresses(IpAttemptActivitySummary summary, AdminLoginAttempt attempt)
    {
        IpAddressPair pair = GetAttemptPair(attempt);

        if (string.IsNullOrWhiteSpace(summary.IpAddressV4) && !string.IsNullOrWhiteSpace(pair.Ipv4))
        {
            summary.IpAddressV4 = pair.Ipv4;
        }

        if (string.IsNullOrWhiteSpace(summary.IpAddressV6) && !string.IsNullOrWhiteSpace(pair.Ipv6))
        {
            summary.IpAddressV6 = pair.Ipv6;
        }
    }

    private static IpAddressPair GetAttemptPair(AdminLoginAttempt attempt)
    {
        if (!string.IsNullOrWhiteSpace(attempt.IpAddressV4) || !string.IsNullOrWhiteSpace(attempt.IpAddressV6))
        {
            return new IpAddressPair(
                string.IsNullOrWhiteSpace(attempt.IpAddressV4) ? null : attempt.IpAddressV4,
                string.IsNullOrWhiteSpace(attempt.IpAddressV6) ? null : attempt.IpAddressV6);
        }

        return IpAddressFormatting.FromString(attempt.IpAddress);
    }
}
