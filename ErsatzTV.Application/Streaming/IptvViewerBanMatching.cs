using ErsatzTV.Application.Security;
using ErsatzTV.Core.Streaming;

namespace ErsatzTV.Application.Streaming;

internal static class IptvViewerBanMatching
{
    public static bool IsSessionIptvBlocked(
        IptvStreamViewerSession session,
        System.Collections.Generic.HashSet<string> iptvBlockedAddresses) =>
        iptvBlockedAddresses.Contains(session.IpAddress) ||
        iptvBlockedAddresses.Contains(session.IpAddressV4) ||
        iptvBlockedAddresses.Contains(session.IpAddressV6);

    public static bool IsSessionLoginBanned(
        IptvStreamViewerSession session,
        System.Collections.Generic.HashSet<string> loginBannedAddresses) =>
        loginBannedAddresses.Contains(session.IpAddress) ||
        loginBannedAddresses.Contains(session.IpAddressV4) ||
        loginBannedAddresses.Contains(session.IpAddressV6);

    public static System.Collections.Generic.HashSet<string> GetIptvBlockedAddresses(
        IEnumerable<string> blacklistRuleAddresses) =>
        BannedIpAttemptMatching.ExpandRuleAddresses(
            blacklistRuleAddresses.Where(address => !string.IsNullOrWhiteSpace(address)));

    public static System.Collections.Generic.HashSet<string> GetLoginBannedAddresses(
        IEnumerable<string> blacklistRuleAddresses) =>
        BannedIpAttemptMatching.ExpandRuleAddresses(
            blacklistRuleAddresses.Where(address => !string.IsNullOrWhiteSpace(address)));
}
