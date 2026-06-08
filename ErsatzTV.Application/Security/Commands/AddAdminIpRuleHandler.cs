using System.Net;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Streaming;
using ErsatzTV.Core.Networking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class AddAdminIpRuleHandler(
    IDbContextFactory<TvContext> dbContextFactory,
    IIptvStreamViewerTracker viewerTracker)
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

        if (ProtectedIpExemption.IsExempt(ipAddress))
        {
            return BaseError.New(
                "Localhost, server network, and base URL IP addresses cannot be added to IP rules.");
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

        AdminIpRule existing = await dbContext.AdminIpRules
            .FirstOrDefaultAsync(
                r => r.IpAddress == ipAddress && r.RuleType == request.RuleType,
                cancellationToken);

        if (existing is not null)
        {
            if (request.BlockIptvStreaming && !existing.BlockIptvStreaming)
            {
                existing.BlockIptvStreaming = true;
                if (!string.IsNullOrWhiteSpace(request.Note))
                {
                    existing.Note = request.Note.Trim();
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                viewerTracker.RemoveSessionsMatchingIp(ipAddress);
                return Unit.Default;
            }

            return BaseError.New("This IP address is already in the list.");
        }

        dbContext.AdminIpRules.Add(
            new AdminIpRule
            {
                IpAddress = ipAddress,
                RuleType = request.RuleType,
                Note = request.Note?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                BlockIptvStreaming = request.BlockIptvStreaming
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.BlockIptvStreaming)
        {
            viewerTracker.RemoveSessionsMatchingIp(ipAddress);
        }

        return Unit.Default;
    }

    private static string NormalizeIp(string ipAddress) => ipAddress?.Trim() ?? string.Empty;
}
