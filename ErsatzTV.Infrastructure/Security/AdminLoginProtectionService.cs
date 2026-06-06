using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using ErsatzTV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ErsatzTV.Infrastructure.Security;

public class AdminLoginProtectionService(
    IDbContextFactory<TvContext> dbContextFactory,
    IConfigElementRepository configElementRepository,
    IAnonymousIpDetectionService anonymousIpDetectionService) : IAdminLoginProtectionService
{
    private const int DefaultMaxFailedAttempts = 5;
    private const int DefaultWindowSeconds = 300;
    private const int DefaultLockoutSeconds = 900;

    public async Task<AdminLoginAccessResult> CheckAccessAsync(
        IpAddressPair clientIp,
        CancellationToken cancellationToken)
    {
        if (ProtectedIpExemption.IsExempt(clientIp))
        {
            return new AdminLoginAccessResult(true, null);
        }

        if (AdminVpnBlockSettings.IsEnabled)
        {
            AnonymousIpLookupResult vpnLookup =
                AnonymousIpDetectionHelper.LookupClientIp(anonymousIpDetectionService, clientIp);
            if (vpnLookup.IsBlocked)
            {
                return new AdminLoginAccessResult(false, vpnLookup.DenyReason);
            }
        }

        LoginIpSettings settings = await GetSettings(cancellationToken);

        if (settings.WhitelistEnabled)
        {
            await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            List<string> whitelist = await dbContext.AdminIpRules.AsNoTracking()
                .Where(r => r.RuleType == AdminIpRuleType.Whitelist)
                .Select(r => r.IpAddress)
                .ToListAsync(cancellationToken);

            if (whitelist.Count > 0 && !whitelist.Any(rule => IpAddressFormatting.MatchesRule(rule, clientIp)))
            {
                return new AdminLoginAccessResult(false, "IP address is not on the whitelist.");
            }
        }

        if (settings.BlacklistEnabled)
        {
            bool isBlacklisted = await IsIpInRules(clientIp, AdminIpRuleType.Blacklist, cancellationToken);
            if (isBlacklisted)
            {
                return new AdminLoginAccessResult(false, "IP address is banned.");
            }
        }

        if (settings.RateLimitEnabled)
        {
            AdminLoginAccessResult rateLimitResult =
                await CheckRateLimit(clientIp, settings, cancellationToken);
            if (!rateLimitResult.Allowed)
            {
                return rateLimitResult;
            }
        }

        return new AdminLoginAccessResult(true, null);
    }

    public async Task RecordAttemptAsync(
        IpAddressPair clientIp,
        string username,
        bool success,
        string userAgent,
        string denyReason,
        AdminLoginAttemptKind attemptKind = AdminLoginAttemptKind.Login,
        string requestPath = "",
        double? latitude = null,
        double? longitude = null,
        double? locationAccuracyMeters = null,
        CancellationToken cancellationToken = default)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.AdminLoginAttempts.Add(
            new AdminLoginAttempt
            {
                Timestamp = DateTime.UtcNow,
                IpAddress = clientIp.Canonical,
                IpAddressV4 = clientIp.Ipv4 ?? string.Empty,
                IpAddressV6 = clientIp.Ipv6 ?? string.Empty,
                Username = username ?? string.Empty,
                Success = success,
                DenyReason = denyReason ?? string.Empty,
                UserAgent = userAgent ?? string.Empty,
                AttemptKind = attemptKind,
                RequestPath = requestPath ?? string.Empty,
                Latitude = latitude,
                Longitude = longitude,
                LocationAccuracyMeters = locationAccuracyMeters
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        await PruneOldAttempts(dbContext, cancellationToken);
    }

    private async Task<LoginIpSettings> GetSettings(CancellationToken cancellationToken) =>
        new()
        {
            RateLimitEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpRateLimitEnabled, cancellationToken)
                .IfNoneAsync(true),
            MaxFailedAttempts = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpMaxFailedAttempts, cancellationToken)
                .IfNoneAsync(DefaultMaxFailedAttempts),
            WindowSeconds = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpWindowSeconds, cancellationToken)
                .IfNoneAsync(DefaultWindowSeconds),
            LockoutSeconds = await configElementRepository
                .GetValue<int>(ConfigElementKey.AdminLoginIpLockoutSeconds, cancellationToken)
                .IfNoneAsync(DefaultLockoutSeconds),
            WhitelistEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpWhitelistEnabled, cancellationToken)
                .IfNoneAsync(false),
            BlacklistEnabled = await configElementRepository
                .GetValue<bool>(ConfigElementKey.AdminLoginIpBlacklistEnabled, cancellationToken)
                .IfNoneAsync(true)
        };

    private async Task<bool> IsIpInRules(
        IpAddressPair clientIp,
        AdminIpRuleType ruleType,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> rules = await dbContext.AdminIpRules.AsNoTracking()
            .Where(r => r.RuleType == ruleType)
            .Select(r => r.IpAddress)
            .ToListAsync(cancellationToken);

        return rules.Any(rule => IpAddressFormatting.MatchesRule(rule, clientIp));
    }

    private async Task<AdminLoginAccessResult> CheckRateLimit(
        IpAddressPair clientIp,
        LoginIpSettings settings,
        CancellationToken cancellationToken)
    {
        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        DateTime windowStart = DateTime.UtcNow.AddSeconds(-settings.WindowSeconds);

        List<AdminLoginAttempt> recentFailures = await dbContext.AdminLoginAttempts.AsNoTracking()
            .Where(a => !a.Success &&
                        a.AttemptKind == AdminLoginAttemptKind.Login &&
                        a.Timestamp >= windowStart)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        List<DateTime> matchingFailures = recentFailures
            .Where(a => AttemptMatchesClient(a, clientIp))
            .Select(a => a.Timestamp)
            .ToList();

        if (matchingFailures.Count < settings.MaxFailedAttempts)
        {
            return new AdminLoginAccessResult(true, null);
        }

        DateTime lastFailure = matchingFailures[0];
        DateTime lockoutEnds = lastFailure.AddSeconds(settings.LockoutSeconds);

        if (DateTime.UtcNow < lockoutEnds)
        {
            return new AdminLoginAccessResult(
                false,
                $"Too many failed login attempts. Try again after {lockoutEnds.ToLocalTime():g}.");
        }

        return new AdminLoginAccessResult(true, null);
    }

    private static bool AttemptMatchesClient(AdminLoginAttempt attempt, IpAddressPair clientIp)
    {
        IpAddressPair stored = new(
            string.IsNullOrWhiteSpace(attempt.IpAddressV4) ? null : attempt.IpAddressV4,
            string.IsNullOrWhiteSpace(attempt.IpAddressV6) ? null : attempt.IpAddressV6);

        if (!string.IsNullOrWhiteSpace(stored.Ipv4) || !string.IsNullOrWhiteSpace(stored.Ipv6))
        {
            return IpAddressFormatting.MatchesRule(stored.Canonical, clientIp) ||
                   (!string.IsNullOrWhiteSpace(stored.Ipv4) &&
                    IpAddressFormatting.MatchesRule(stored.Ipv4, clientIp)) ||
                   (!string.IsNullOrWhiteSpace(stored.Ipv6) &&
                    IpAddressFormatting.MatchesRule(stored.Ipv6, clientIp));
        }

        return IpAddressFormatting.MatchesRule(attempt.IpAddress, clientIp);
    }

    private static async Task PruneOldAttempts(TvContext dbContext, CancellationToken cancellationToken)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-90);

        await dbContext.AdminLoginAttempts
            .Where(a => a.Timestamp < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private sealed class LoginIpSettings
    {
        public bool RateLimitEnabled { get; init; }
        public int MaxFailedAttempts { get; init; }
        public int WindowSeconds { get; init; }
        public int LockoutSeconds { get; init; }
        public bool WhitelistEnabled { get; init; }
        public bool BlacklistEnabled { get; init; }
    }
}
