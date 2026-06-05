using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public record AddAdminIpRule(string IpAddress, AdminIpRuleType RuleType, string Note)
    : IRequest<Either<BaseError, Unit>>;
