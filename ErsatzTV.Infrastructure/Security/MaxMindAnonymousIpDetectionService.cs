using System.Net;
using System.Net.Sockets;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;
using MaxMind.Db;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ErsatzTV.Infrastructure.Security;

public sealed class MaxMindAnonymousIpDetectionService : IAnonymousIpDetectionService, IDisposable
{
    private readonly Reader _reader;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(6);

    public MaxMindAnonymousIpDetectionService(IMemoryCache cache)
    {
        _cache = cache;
        string databasePath = AdminVpnBlockSettings.DatabasePath;

        if (!File.Exists(databasePath))
        {
            DatabasePath = databasePath;
            return;
        }

        try
        {
            _reader = new Reader(databasePath, FileAccessMode.MemoryMapped);
            DatabasePath = databasePath;
        }
        catch (Exception ex)
        {
            DatabasePath = databasePath;
            Log.Logger.Error(ex, "Failed to load anonymous IP database from {DatabasePath}", databasePath);
        }
    }

    public bool IsConfigured => _reader is not null;

    public string DatabasePath { get; private set; } = string.Empty;

    public AnonymousIpLookupResult Lookup(string ipAddress)
    {
        if (!AdminVpnBlockSettings.IsEnabled)
        {
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }

        if (!IsConfigured)
        {
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }

        if (string.IsNullOrWhiteSpace(ipAddress) ||
            !IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed))
        {
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }

        if (parsed.AddressFamily == AddressFamily.InterNetworkV6 && parsed.IsIPv4MappedToIPv6)
        {
            parsed = parsed.MapToIPv4();
        }

        string cacheKey = $"anonymous-ip:{parsed}";
        if (_cache.TryGetValue(cacheKey, out AnonymousIpLookupResult cached))
        {
            return cached;
        }

        AnonymousIpLookupResult result = LookupCore(parsed);
        _cache.Set(cacheKey, result, _cacheDuration);
        return result;
    }

    private AnonymousIpLookupResult LookupCore(IPAddress ipAddress)
    {
        try
        {
            AnonymousIpDatabaseRecord record = _reader.Find<AnonymousIpDatabaseRecord>(ipAddress);
            if (record is null)
            {
                return new AnonymousIpLookupResult(false, null, true, false, false, false);
            }

            bool blocked = AdminVpnBlockSettings.ShouldBlock(
                record.IsAnonymousVpn,
                record.IsPublicProxy,
                record.IsTorExitNode,
                anonymous: record.IsAnonymous);

            if (!blocked)
            {
                return new AnonymousIpLookupResult(false, null, true, record.IsAnonymousVpn, record.IsPublicProxy, record.IsTorExitNode);
            }

            string reason = BuildDenyReason(record);
            return new AnonymousIpLookupResult(
                true,
                reason,
                true,
                record.IsAnonymousVpn,
                record.IsPublicProxy,
                record.IsTorExitNode);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Anonymous IP lookup failed for {IpAddress}", ipAddress);
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }
    }

    private static string BuildDenyReason(AnonymousIpDatabaseRecord record)
    {
        if (record.IsAnonymousVpn)
        {
            return "VPN connections are not allowed.";
        }

        if (record.IsTorExitNode)
        {
            return "Tor connections are not allowed.";
        }

        if (record.IsPublicProxy)
        {
            return "Proxy connections are not allowed.";
        }

        return "Anonymous connections are not allowed.";
    }

    public void Dispose() => _reader?.Dispose();

    private sealed class AnonymousIpDatabaseRecord
    {
        public AnonymousIpDatabaseRecord()
        {
        }

        [Constructor]
        public AnonymousIpDatabaseRecord(
            [Parameter("is_anonymous")] bool isAnonymous = false,
            [Parameter("is_anonymous_vpn")] bool isAnonymousVpn = false,
            [Parameter("is_public_proxy")] bool isPublicProxy = false,
            [Parameter("is_tor_exit_node")] bool isTorExitNode = false,
            [Parameter("is_hosting_provider")] bool isHostingProvider = false)
        {
            IsAnonymous = isAnonymous;
            IsAnonymousVpn = isAnonymousVpn;
            IsPublicProxy = isPublicProxy;
            IsTorExitNode = isTorExitNode;
            IsHostingProvider = isHostingProvider;
        }

        public bool IsAnonymous { get; }
        public bool IsAnonymousVpn { get; }
        public bool IsPublicProxy { get; }
        public bool IsTorExitNode { get; }
        public bool IsHostingProvider { get; }
    }
}
