using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public record GetAdminLoginAttempts(
    int PageNum,
    int PageSize,
    string Search,
    bool? SuccessFilter,
    AdminLoginAttemptKind? AttemptKindFilter = null,
    string SortLabel = "timestamp",
    bool SortDescending = true,
    LoginAttemptScope Scope = LoginAttemptScope.Active,
    string IpAddressFilter = null) : IRequest<PagedAdminLoginAttemptsViewModel>;
