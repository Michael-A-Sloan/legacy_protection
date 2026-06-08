using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public record UnbanIptvViewer(string IpAddress) : IRequest<Either<BaseError, Unit>>;

public class UnbanIptvViewerHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UnbanIptvViewer, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        UnbanIptvViewer request,
        CancellationToken cancellationToken)
    {
        string ipAddress = request.IpAddress?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BaseError.New("IP address is required.");
        }

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AdminIpRule rule = await dbContext.AdminIpRules
            .FirstOrDefaultAsync(
                r => r.IpAddress == ipAddress && r.RuleType == AdminIpRuleType.Blacklist,
                cancellationToken);

        if (rule is null)
        {
            return BaseError.New("IP address is not banned.");
        }

        if (!rule.BlockIptvStreaming)
        {
            return BaseError.New("This IP is not blocked from IPTV streaming.");
        }

        rule.BlockIptvStreaming = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }
}
