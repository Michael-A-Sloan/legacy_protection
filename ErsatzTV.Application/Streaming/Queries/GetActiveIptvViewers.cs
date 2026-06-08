using ErsatzTV.Application.Security;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using ErsatzTV.Core.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public record GetActiveIptvViewers : IRequest<List<ActiveIptvViewerViewModel>>;

public class GetActiveIptvViewersHandler(
    IIptvStreamViewerTracker viewerTracker,
    IAnonymousIpDetectionService anonymousIpDetectionService,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetActiveIptvViewers, List<ActiveIptvViewerViewModel>>
{
    public async Task<List<ActiveIptvViewerViewModel>> Handle(
        GetActiveIptvViewers request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IptvStreamViewerSession> sessions =
            viewerTracker.GetActiveViewers(viewerTracker.ActivityWindow);

        if (sessions.Count == 0)
        {
            return [];
        }

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Dictionary<string, string> channelNames = await dbContext.Channels.AsNoTracking()
            .Select(channel => new { channel.Number, channel.Name })
            .ToDictionaryAsync(
                channel => channel.Number,
                channel => channel.Name,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        List<AdminIpRule> blacklistRules = await dbContext.AdminIpRules.AsNoTracking()
            .Where(rule => rule.RuleType == AdminIpRuleType.Blacklist)
            .ToListAsync(cancellationToken);

        System.Collections.Generic.HashSet<string> loginBannedAddresses =
            IptvViewerBanMatching.GetLoginBannedAddresses(blacklistRules.Select(rule => rule.IpAddress));

        System.Collections.Generic.HashSet<string> iptvBlockedAddresses =
            IptvViewerBanMatching.GetIptvBlockedAddresses(
                blacklistRules.Where(rule => rule.BlockIptvStreaming).Select(rule => rule.IpAddress));

        bool vpnLookupAvailable = anonymousIpDetectionService.IsConfigured;
        var metadataByIp = new Dictionary<string, AnonymousIpLookupResult>(StringComparer.OrdinalIgnoreCase);

        var viewers = new List<ActiveIptvViewerViewModel>(sessions.Count);

        foreach (IptvStreamViewerSession session in sessions)
        {
            if (IptvViewerBanMatching.IsSessionIptvBlocked(session, iptvBlockedAddresses))
            {
                continue;
            }

            IpAddressPair clientIp = new(
                string.IsNullOrWhiteSpace(session.IpAddressV4) ? null : session.IpAddressV4,
                string.IsNullOrWhiteSpace(session.IpAddressV6) ? null : session.IpAddressV6);

            if (string.IsNullOrWhiteSpace(clientIp.Canonical) || clientIp.Canonical == "unknown")
            {
                clientIp = IpAddressFormatting.FromString(session.IpAddress);
            }

            bool isLoginBanned = IptvViewerBanMatching.IsSessionLoginBanned(session, loginBannedAddresses);

            bool isVpn = false;
            bool isProxy = false;
            bool isTor = false;

            if (vpnLookupAvailable)
            {
                AnonymousIpLookupResult metadata = GetMetadata(metadataByIp, anonymousIpDetectionService, clientIp);
                isVpn = metadata.IsVpn;
                isProxy = metadata.IsProxy;
                isTor = metadata.IsTor;
            }

            viewers.Add(new ActiveIptvViewerViewModel
            {
                ChannelNumber = session.ChannelNumber,
                ChannelName = channelNames.TryGetValue(session.ChannelNumber, out string channelName)
                    ? channelName
                    : string.Empty,
                IpAddress = session.IpAddress,
                IpAddressV4 = session.IpAddressV4,
                IpAddressV6 = session.IpAddressV6,
                UserAgent = session.UserAgent,
                LastActivityUtc = session.LastActivityUtc,
                IsVpn = isVpn,
                IsProxy = isProxy,
                IsTor = isTor,
                IsLoginBanned = isLoginBanned,
                IsBlacklisted = false,
                CanBan = !ProtectedIpExemption.IsExempt(session.IpAddress) &&
                         !ProtectedIpExemption.IsExempt(clientIp)
            });
        }

        return viewers;
    }

    private static AnonymousIpLookupResult GetMetadata(
        Dictionary<string, AnonymousIpLookupResult> metadataByIp,
        IAnonymousIpDetectionService anonymousIpDetectionService,
        IpAddressPair clientIp)
    {
        string lookupKey = clientIp.Canonical;
        if (metadataByIp.TryGetValue(lookupKey, out AnonymousIpLookupResult cached))
        {
            return cached;
        }

        AnonymousIpLookupResult metadata =
            AnonymousIpDetectionHelper.LookupClientIpMetadata(anonymousIpDetectionService, clientIp);
        metadataByIp[lookupKey] = metadata;
        return metadata;
    }
}
