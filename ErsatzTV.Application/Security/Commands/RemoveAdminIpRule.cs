namespace ErsatzTV.Application.Security;

public record RemoveAdminIpRule(int Id) : IRequest<Either<BaseError, Unit>>;
