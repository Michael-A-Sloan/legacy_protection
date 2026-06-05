using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public record UnbanIpAddress(string IpAddress) : IRequest<Either<BaseError, Unit>>;

public class UnbanIpAddressHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<UnbanIpAddress, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        UnbanIpAddress request,
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

        dbContext.AdminIpRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }
}
