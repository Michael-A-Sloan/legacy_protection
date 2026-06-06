using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Application.Security;

public class GetAdminLoginAttemptsHandler(IDbContextFactory<TvContext> dbContextFactory)
    : IRequestHandler<GetAdminLoginAttempts, PagedAdminLoginAttemptsViewModel>
{
    public async Task<PagedAdminLoginAttemptsViewModel> Handle(
        GetAdminLoginAttempts request,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AdminLoginAttempt> query = dbContext.AdminLoginAttempts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string search = request.Search.Trim();
            query = query.Where(a =>
                a.IpAddress.Contains(search) ||
                a.IpAddressV4.Contains(search) ||
                a.IpAddressV6.Contains(search) ||
                a.Username.Contains(search) ||
                a.DenyReason.Contains(search) ||
                a.UserAgent.Contains(search) ||
                a.RequestPath.Contains(search));
        }

        if (request.SuccessFilter.HasValue)
        {
            query = query.Where(a => a.Success == request.SuccessFilter.Value);
        }

        if (request.AttemptKindFilter.HasValue)
        {
            query = query.Where(a => a.AttemptKind == request.AttemptKindFilter.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, request.SortLabel, request.SortDescending);

        List<AdminLoginAttempt> attempts = await query
            .Skip(request.PageNum * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        List<AdminLoginAttemptViewModel> page = attempts.Select(MapAttempt).ToList();

        return new PagedAdminLoginAttemptsViewModel(totalCount, page);
    }

    private static AdminLoginAttemptViewModel MapAttempt(AdminLoginAttempt attempt)
    {
        string ipv4 = attempt.IpAddressV4;
        string ipv6 = attempt.IpAddressV6;

        if (string.IsNullOrWhiteSpace(ipv4) && string.IsNullOrWhiteSpace(ipv6))
        {
            IpAddressPair pair = IpAddressFormatting.FromString(attempt.IpAddress);
            ipv4 = pair.Ipv4 ?? string.Empty;
            ipv6 = pair.Ipv6 ?? string.Empty;
        }

        return new AdminLoginAttemptViewModel
        {
            Id = attempt.Id,
            Timestamp = attempt.Timestamp,
            IpAddress = attempt.IpAddress,
            IpAddressV4 = ipv4,
            IpAddressV6 = ipv6,
            Username = attempt.Username,
            Success = attempt.Success,
            DenyReason = attempt.DenyReason,
            UserAgent = attempt.UserAgent,
            AttemptKind = attempt.AttemptKind,
            RequestPath = attempt.RequestPath,
            Latitude = attempt.Latitude,
            Longitude = attempt.Longitude,
            LocationAccuracyMeters = attempt.LocationAccuracyMeters
        };
    }

    private static IQueryable<AdminLoginAttempt> ApplySort(
        IQueryable<AdminLoginAttempt> query,
        string sortLabel,
        bool descending) =>
        sortLabel?.ToLowerInvariant() switch
        {
            "ipaddress" or "ipaddressv4" => descending
                ? query.OrderByDescending(a => a.IpAddressV4).ThenByDescending(a => a.IpAddress)
                : query.OrderBy(a => a.IpAddressV4).ThenBy(a => a.IpAddress),
            "ipaddressv6" => descending
                ? query.OrderByDescending(a => a.IpAddressV6).ThenByDescending(a => a.IpAddress)
                : query.OrderBy(a => a.IpAddressV6).ThenBy(a => a.IpAddress),
            "username" => descending
                ? query.OrderByDescending(a => a.Username)
                : query.OrderBy(a => a.Username),
            "success" => descending
                ? query.OrderByDescending(a => a.Success)
                : query.OrderBy(a => a.Success),
            "attemptkind" => descending
                ? query.OrderByDescending(a => a.AttemptKind)
                : query.OrderBy(a => a.AttemptKind),
            "requestpath" => descending
                ? query.OrderByDescending(a => a.RequestPath)
                : query.OrderBy(a => a.RequestPath),
            _ => descending
                ? query.OrderByDescending(a => a.Timestamp)
                : query.OrderBy(a => a.Timestamp)
        };
}
