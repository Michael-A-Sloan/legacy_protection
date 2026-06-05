using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public record GetAdminIpRules(AdminIpRuleType? RuleType) : IRequest<List<AdminIpRuleViewModel>>;
