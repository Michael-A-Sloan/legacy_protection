using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public record AddAdminIpRule(
    string IpAddress,
    AdminIpRuleType RuleType,
    string Note,
    bool BlockIptvStreaming = false)
    : IRequest<Either<BaseError, Unit>>;
