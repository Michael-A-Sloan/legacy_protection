using System.Net;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class AddAdminIpRuleHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<AddAdminIpRule, Either<BaseError, Unit>>
{
    public async Task<Either<BaseError, Unit>> Handle(
        AddAdminIpRule request,
        CancellationToken cancellationToken)
    {
        string ipAddress = NormalizeIp(request.IpAddress);
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BaseError.New("IP address is required.");
        }

        if (LocalIpExemption.IsExempt(ipAddress))
        {
            return BaseError.New("Local addresses (127.0.0.1, ::1, 0.0.0.0) cannot be added to IP rules.");
        }

        IpAddressPair pair = IpAddressFormatting.FromString(ipAddress);
        ipAddress = pair.Canonical;

        if (!IPAddress.TryParse(ipAddress, out _) &&
            string.IsNullOrWhiteSpace(pair.Ipv4) &&
            string.IsNullOrWhiteSpace(pair.Ipv6))
        {
            return BaseError.New("Invalid IP address format.");
        }

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        bool exists = await dbContext.AdminIpRules
            .AnyAsync(r => r.IpAddress == ipAddress && r.RuleType == request.RuleType, cancellationToken);

        if (exists)
        {
            return BaseError.New("This IP address is already in the list.");
        }

        dbContext.AdminIpRules.Add(
            new AdminIpRule
            {
                IpAddress = ipAddress,
                RuleType = request.RuleType,
                Note = request.Note?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            });

        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Default;
    }

    private static string NormalizeIp(string ipAddress) => ipAddress?.Trim() ?? string.Empty;
}
