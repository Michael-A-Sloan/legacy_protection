using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class RemoveAdminIpRuleHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<RemoveAdminIpRule, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        RemoveAdminIpRule request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AdminIpRule rule = await dbContext.AdminIpRules
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (rule is null)
        {
            return BaseError.New("IP rule not found.");
        }

        dbContext.AdminIpRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }
}
