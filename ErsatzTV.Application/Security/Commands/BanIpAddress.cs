using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public record BanIpAddress(string IpAddress, string Note) : IRequest<Either<BaseError, Unit>>;

public class BanIpAddressHandler(IMediator mediator) : IRequestHandler<BanIpAddress, Either<BaseError, Unit>>
{
    public Task<Either<BaseError, Unit>> Handle(BanIpAddress request, CancellationToken cancellationToken) =>
        mediator.Send(
            new AddAdminIpRule(request.IpAddress, AdminIpRuleType.Blacklist, request.Note),
            cancellationToken);
}
