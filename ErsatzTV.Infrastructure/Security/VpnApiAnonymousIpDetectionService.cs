using System.Net;
using System.Text.Json.Serialization;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ErsatzTV.Infrastructure.Security;

public sealed class VpnApiAnonymousIpDetectionService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    : IAnonymousIpDetectionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public bool IsConfigured => AdminVpnBlockSettings.IsApiConfigured;

    public string DatabasePath => "vpnapi.io API";

    public AnonymousIpLookupResult Lookup(string ipAddress)
    {
        if (!AdminVpnBlockSettings.IsEnabled || !IsConfigured)
        {
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }

        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed))
        {
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }

        string normalized = parsed.ToString();
        string cacheKey = $"vpnapi:{normalized}";
        if (cache.TryGetValue(cacheKey, out AnonymousIpLookupResult cached))
        {
            return cached;
        }

        AnonymousIpLookupResult result = LookupCore(normalized);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private AnonymousIpLookupResult LookupCore(string ipAddress)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("VpnApi");
            string requestUri =
                $"api/{Uri.EscapeDataString(ipAddress)}?key={Uri.EscapeDataString(AdminVpnBlockSettings.ApiKey)}";

            using HttpResponseMessage response = client.GetAsync(requestUri).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Log.Logger.Warning(
                    "vpnapi.io lookup failed for {IpAddress} with status {StatusCode}",
                    ipAddress,
                    (int)response.StatusCode);
                return new AnonymousIpLookupResult(false, null, false, false, false, false);
            }

            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            VpnApiResponse payload = System.Text.Json.JsonSerializer.Deserialize<VpnApiResponse>(json);
            VpnApiSecurity security = payload?.Security;
            if (security is null)
            {
                return new AnonymousIpLookupResult(false, null, false, false, false, false);
            }

            bool blocked = AdminVpnBlockSettings.ShouldBlock(security.Vpn, security.Proxy, security.Tor, security.Relay);
            if (!blocked)
            {
                return new AnonymousIpLookupResult(
                    false,
                    null,
                    true,
                    security.Vpn,
                    security.Proxy,
                    security.Tor);
            }

            return new AnonymousIpLookupResult(
                true,
                BuildDenyReason(security),
                true,
                security.Vpn,
                security.Proxy,
                security.Tor);
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "vpnapi.io lookup failed for {IpAddress}", ipAddress);
            return new AnonymousIpLookupResult(false, null, false, false, false, false);
        }
    }

    private static string BuildDenyReason(VpnApiSecurity security)
    {
        if (security.Vpn)
        {
            return "VPN connections are not allowed.";
        }

        if (security.Tor)
        {
            return "Tor connections are not allowed.";
        }

        if (security.Proxy)
        {
            return "Proxy connections are not allowed.";
        }

        if (security.Relay)
        {
            return "Relay connections are not allowed.";
        }

        return "Anonymous connections are not allowed.";
    }

    private sealed class VpnApiResponse
    {
        [JsonPropertyName("security")]
        public VpnApiSecurity Security { get; set; }
    }

    private sealed class VpnApiSecurity
    {
        [JsonPropertyName("vpn")]
        public bool Vpn { get; set; }

        [JsonPropertyName("proxy")]
        public bool Proxy { get; set; }

        [JsonPropertyName("tor")]
        public bool Tor { get; set; }

        [JsonPropertyName("relay")]
        public bool Relay { get; set; }
    }
}
