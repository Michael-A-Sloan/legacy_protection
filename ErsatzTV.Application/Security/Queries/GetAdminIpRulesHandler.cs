using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class GetAdminIpRulesHandler(IDbContextFactory<TvContext> dbContextFactory)
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

        return rules.Select(r =>
        {
            IpAddressPair pair = IpAddressFormatting.FromString(r.IpAddress);
            return new AdminIpRuleViewModel
            {
                Id = r.Id,
                IpAddress = r.IpAddress,
                IpAddressV4 = pair.Ipv4 ?? string.Empty,
                IpAddressV6 = pair.Ipv6 ?? string.Empty,
                RuleType = r.RuleType,
                Note = r.Note,
                CreatedAt = r.CreatedAt
            };
        }).ToList();
    }
}
