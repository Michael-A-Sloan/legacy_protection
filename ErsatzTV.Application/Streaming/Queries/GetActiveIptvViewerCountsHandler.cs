using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Streaming;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Streaming;

public class GetActiveIptvViewerCountsHandler(
    IIptvStreamViewerTracker viewerTracker,
    IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetActiveIptvViewerCounts, IReadOnlyDictionary<string, int>>
{
    public async Task<IReadOnlyDictionary<string, int>> Handle(
        GetActiveIptvViewerCounts request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IptvStreamViewerSession> sessions =
            viewerTracker.GetActiveViewers(viewerTracker.ActivityWindow);

        if (sessions.Count == 0)
        {
            return new Dictionary<string, int>();
        }

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> iptvBlockRules = await dbContext.AdminIpRules.AsNoTracking()
            .Where(rule => rule.RuleType == AdminIpRuleType.Blacklist && rule.BlockIptvStreaming)
            .Select(rule => rule.IpAddress)
            .ToListAsync(cancellationToken);

        System.Collections.Generic.HashSet<string> iptvBlockedAddresses =
            IptvViewerBanMatching.GetIptvBlockedAddresses(iptvBlockRules);

        return sessions
            .Where(session => !IptvViewerBanMatching.IsSessionIptvBlocked(session, iptvBlockedAddresses))
            .GroupBy(session => session.ChannelNumber, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }
}
