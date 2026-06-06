namespace ErsatzTV.Application.Security;

public record ClearAdminLoginAttempts(LoginAttemptScope Scope = LoginAttemptScope.Active)
    : IRequest<Either<BaseError, int>>;
