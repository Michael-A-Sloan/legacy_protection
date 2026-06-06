using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class ClearAdminLoginAttemptsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<ClearAdminLoginAttempts, Either<BaseError, int>>
{
    public async Task<Either<BaseError, int>> Handle(
        ClearAdminLoginAttempts request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> blacklistRules =
            await BannedIpAttemptMatching.GetBlacklistRuleAddresses(dbContext, cancellationToken);
        System.Collections.Generic.HashSet<string> bannedAddresses =
            BannedIpAttemptMatching.ExpandRuleAddresses(blacklistRules);

        IQueryable<AdminLoginAttempt> query = dbContext.AdminLoginAttempts;
        query = BannedIpAttemptMatching.ApplyScope(query, request.Scope, bannedAddresses);

        int deleted = await query.ExecuteDeleteAsync(cancellationToken);
        return deleted;
    }
}
