using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Domain.Security;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using ErsatzTV.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ErsatzTV.Application.Security;

public sealed class LoginIpAutoBanScanner(
    IDbContextFactory<TvContext> dbContextFactory,
    IConfigElementRepository configElementRepository,
    IPublicBlocklistService publicBlocklistService,
    IAbuseIpDbDetectionService abuseIpDbDetectionService,
    IAnonymousIpDetectionService anonymousIpDetectionService,
    IMediator mediator) : ILoginIpAutoBanScanner
{
    public async Task<LoginIpAutoBanScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        LoginIpAutoBanSettings settings =
            await LoginIpAutoBanSettings.LoadAsync(configElementRepository, cancellationToken);

        if (!settings.IsAnyScanEnabled)
        {
            return new LoginIpAutoBanScanResult(0, 0, 0, []);
        }

        bool blacklistEnabled = await configElementRepository
            .GetValue<bool>(ConfigElementKey.AdminLoginIpBlacklistEnabled, cancellationToken)
            .IfNoneAsync(true);

        if (!blacklistEnabled)
        {
            Log.Logger.Information("Login IP auto-ban scan skipped because the blacklist is disabled");
            return new LoginIpAutoBanScanResult(0, 0, 0, []);
        }

        await using TvContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<string> blacklistRules =
            await BannedIpAttemptMatching.GetBlacklistRuleAddresses(dbContext, cancellationToken);
        System.Collections.Generic.HashSet<string> bannedAddresses =
            BannedIpAttemptMatching.ExpandRuleAddresses(blacklistRules);

        List<string> whitelistRules = await dbContext.AdminIpRules.AsNoTracking()
            .Where(r => r.RuleType == AdminIpRuleType.Whitelist)
            .Select(r => r.IpAddress)
            .ToListAsync(cancellationToken);

        IQueryable<AdminLoginAttempt> query = dbContext.AdminLoginAttempts.AsNoTracking();
        query = BannedIpAttemptMatching.ApplyScope(query, LoginAttemptScope.Active, bannedAddresses);

        DateTime? activityCutoff = settings.ActivityEnabled
            ? DateTime.UtcNow.AddDays(-Math.Clamp(settings.ActivityWindowDays, 1, 365))
            : null;

        List<AdminLoginAttempt> attempts = await query.ToListAsync(cancellationToken);
        Dictionary<string, IpAttemptActivitySummary> summaries = IpAttemptActivityAggregator.Aggregate(attempts);

        bool abuseIpDbEnabled =
            await AdminAbuseIpDbSettings.IsBlockingEnabledAsync(configElementRepository, cancellationToken);
        int abuseIpDbMinScore = await AdminAbuseIpDbSettings.GetMinScoreAsync(
            configElementRepository,
            cancellationToken);

        int scannedCount = 0;
        int skippedCount = 0;
        int bannedCount = 0;
        var actions = new List<LoginIpAutoBanAction>();

        foreach (IpAttemptActivitySummary summary in summaries.Values)
        {
            if (string.IsNullOrWhiteSpace(summary.IpAddress) ||
                string.Equals(summary.IpAddress, "unknown", StringComparison.OrdinalIgnoreCase))
            {
                skippedCount++;
                continue;
            }

            if (ProtectedIpExemption.IsExempt(summary.IpAddress))
            {
                skippedCount++;
                continue;
            }

            IpAddressPair clientIp = GetSummaryPair(summary);
            if (ProtectedIpExemption.IsExempt(clientIp))
            {
                skippedCount++;
                continue;
            }

            if (IsWhitelisted(clientIp, whitelistRules))
            {
                skippedCount++;
                continue;
            }

            scannedCount++;

            string banReason = await EvaluateBanReasonAsync(
                settings,
                clientIp,
                summary,
                attempts,
                activityCutoff,
                abuseIpDbEnabled,
                abuseIpDbMinScore,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(banReason))
            {
                continue;
            }

            Either<BaseError, Unit> banResult = await mediator.Send(
                new BanIpAddress(summary.IpAddress, banReason),
                cancellationToken);

            if (banResult.IsRight)
            {
                bannedCount++;
                actions.Add(new LoginIpAutoBanAction(summary.IpAddress, banReason));
                continue;
            }

            foreach (BaseError error in banResult.LeftToSeq())
            {
                Log.Logger.Warning(
                    "Login IP auto-ban failed for {IpAddress}: {Error}",
                    summary.IpAddress,
                    error.Value);
            }

            skippedCount++;
        }

        settings.LastScanUtc = DateTime.UtcNow;
        settings.LastScanScannedCount = scannedCount;
        settings.LastScanBannedCount = bannedCount;
        settings.LastScanSkippedCount = skippedCount;
        await LoginIpAutoBanSettings.SaveAsync(configElementRepository, settings, cancellationToken);

        Log.Logger.Information(
            "Login IP auto-ban scan completed: scanned {Scanned}, banned {Banned}, skipped {Skipped}",
            scannedCount,
            bannedCount,
            skippedCount);

        return new LoginIpAutoBanScanResult(scannedCount, bannedCount, skippedCount, actions);
    }

    private async Task<string> EvaluateBanReasonAsync(
        LoginIpAutoBanSettings settings,
        IpAddressPair clientIp,
        IpAttemptActivitySummary summary,
        List<AdminLoginAttempt> attempts,
        DateTime? activityCutoff,
        bool abuseIpDbEnabled,
        int abuseIpDbMinScore,
        CancellationToken cancellationToken)
    {
        if (settings.ThreatIntelEnabled)
        {
            PublicBlocklistMatchResult blocklistMatch =
                await publicBlocklistService.MatchClientIpAsync(clientIp, cancellationToken);
            if (blocklistMatch.IsBlocked)
            {
                return $"Auto-banned: matched public blocklist {blocklistMatch.ListName}.";
            }

            if (abuseIpDbEnabled &&
                AbuseIpDbDetectionHelper.IsClientIpBlocked(
                    abuseIpDbDetectionService,
                    clientIp,
                    abuseIpDbMinScore,
                    out AbuseIpDbLookupResult blockedLookup))
            {
                return
                    $"Auto-banned: AbuseIPDB score {blockedLookup.AbuseConfidenceScore}% (threshold {abuseIpDbMinScore}).";
            }
        }

        if (settings.ActivityEnabled && activityCutoff.HasValue)
        {
            IEnumerable<AdminLoginAttempt> ipAttempts = attempts.Where(attempt =>
                BannedIpAttemptMatching.MatchesAnyRuleAddress(
                    attempt,
                    BannedIpAttemptMatching.ExpandRuleAddresses([summary.IpAddress])) &&
                attempt.Timestamp >= activityCutoff.Value);

            int failures = LoginIpAutoBanActivityRules.CountQualifyingFailures(
                ipAttempts,
                settings.ActivityIncludeAccessDenied);

            if (LoginIpAutoBanActivityRules.ShouldBanByActivity(
                    failures,
                    Math.Clamp(settings.ActivityMinFailedAttempts, 1, 1000)))
            {
                return
                    $"Auto-banned: {failures} failed login events in the last {settings.ActivityWindowDays} days.";
            }
        }

        if (settings.VpnEnabled &&
            AdminVpnBlockSettings.IsDetectionAvailable &&
            anonymousIpDetectionService.IsConfigured)
        {
            AnonymousIpLookupResult metadata =
                AnonymousIpDetectionHelper.LookupClientIpMetadata(anonymousIpDetectionService, clientIp);
            if (metadata.IsVpn || metadata.IsProxy || metadata.IsTor)
            {
                return BuildVpnAutoBanReason(metadata);
            }
        }

        return string.Empty;
    }

    private static string BuildVpnAutoBanReason(AnonymousIpLookupResult metadata)
    {
        if (metadata.IsTor)
        {
            return "Auto-banned: Tor exit node.";
        }

        if (metadata.IsVpn && metadata.IsProxy)
        {
            return "Auto-banned: VPN and proxy network.";
        }

        if (metadata.IsVpn)
        {
            return "Auto-banned: VPN network.";
        }

        if (metadata.IsProxy)
        {
            return "Auto-banned: proxy network.";
        }

        return "Auto-banned: anonymous network.";
    }

    private static IpAddressPair GetSummaryPair(IpAttemptActivitySummary summary)
    {
        if (!string.IsNullOrWhiteSpace(summary.IpAddressV4) || !string.IsNullOrWhiteSpace(summary.IpAddressV6))
        {
            return new IpAddressPair(
                string.IsNullOrWhiteSpace(summary.IpAddressV4) ? null : summary.IpAddressV4,
                string.IsNullOrWhiteSpace(summary.IpAddressV6) ? null : summary.IpAddressV6);
        }

        return IpAddressFormatting.FromString(summary.IpAddress);
    }

    private static bool IsWhitelisted(IpAddressPair clientIp, IEnumerable<string> whitelistRules) =>
        whitelistRules.Any(rule => IpAddressFormatting.MatchesRule(rule, clientIp));
}
