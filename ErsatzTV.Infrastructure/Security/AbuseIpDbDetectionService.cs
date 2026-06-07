using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace ErsatzTV.Infrastructure.Security;

public sealed class AbuseIpDbDetectionService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    : IAbuseIpDbDetectionService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public bool IsConfigured => AdminAbuseIpDbSettings.IsApiConfigured;

    public AbuseIpDbLookupResult Lookup(string ipAddress)
    {
        if (!AdminAbuseIpDbSettings.IsApiConfigured)
        {
            return AbuseIpDbLookupResult.NotConfigured();
        }

        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed))
        {
            return AbuseIpDbLookupResult.Failed(ipAddress ?? string.Empty, "Invalid IP address.");
        }

        string normalized = parsed.ToString();
        string cacheKey = $"abuseipdb:{normalized}";
        if (cache.TryGetValue(cacheKey, out AbuseIpDbLookupResult cached))
        {
            return cached;
        }

        AbuseIpDbLookupResult result = LookupCore(normalized);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }

    private AbuseIpDbLookupResult LookupCore(string ipAddress)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("AbuseIpDb");
            string requestUri =
                $"check?ipAddress={Uri.EscapeDataString(ipAddress)}&maxAgeInDays={AdminAbuseIpDbSettings.MaxAgeInDays}&verbose";

            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            request.Headers.Add("Key", AdminAbuseIpDbSettings.ApiKey);
            request.Headers.Add("Accept", "application/json");

            using HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Log.Logger.Warning(
                    "AbuseIPDB lookup failed for {IpAddress} with status {StatusCode}",
                    ipAddress,
                    (int)response.StatusCode);
                return AbuseIpDbLookupResult.Failed(
                    ipAddress,
                    $"AbuseIPDB lookup failed ({(int)response.StatusCode}).");
            }

            string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            AbuseIpDbCheckResponse payload =
                System.Text.Json.JsonSerializer.Deserialize<AbuseIpDbCheckResponse>(json);
            AbuseIpDbCheckData data = payload?.Data;
            if (data is null)
            {
                return AbuseIpDbLookupResult.Failed(ipAddress, "AbuseIPDB returned an empty response.");
            }

            return new AbuseIpDbLookupResult(
                true,
                null,
                data.IpAddress ?? ipAddress,
                data.AbuseConfidenceScore,
                data.IsWhitelisted,
                data.IsPublic,
                data.IpVersion,
                data.CountryCode ?? string.Empty,
                data.CountryName ?? string.Empty,
                data.UsageType ?? string.Empty,
                data.Isp ?? string.Empty,
                data.Domain ?? string.Empty,
                data.TotalReports,
                data.NumDistinctUsers,
                ParseLastReportedAt(data.LastReportedAt));
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "AbuseIPDB lookup failed for {IpAddress}", ipAddress);
            return AbuseIpDbLookupResult.Failed(ipAddress, "AbuseIPDB lookup failed.");
        }
    }

    private static DateTimeOffset? ParseLastReportedAt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : null;
    }

    private sealed class AbuseIpDbCheckResponse
    {
        [JsonPropertyName("data")]
        public AbuseIpDbCheckData Data { get; set; }
    }

    private sealed class AbuseIpDbCheckData
    {
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }

        [JsonPropertyName("isPublic")]
        public bool IsPublic { get; set; }

        [JsonPropertyName("ipVersion")]
        public int IpVersion { get; set; }

        [JsonPropertyName("isWhitelisted")]
        public bool IsWhitelisted { get; set; }

        [JsonPropertyName("abuseConfidenceScore")]
        public int AbuseConfidenceScore { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("countryName")]
        public string CountryName { get; set; }

        [JsonPropertyName("usageType")]
        public string UsageType { get; set; }

        [JsonPropertyName("isp")]
        public string Isp { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("totalReports")]
        public int TotalReports { get; set; }

        [JsonPropertyName("numDistinctUsers")]
        public int NumDistinctUsers { get; set; }

        [JsonPropertyName("lastReportedAt")]
        public string LastReportedAt { get; set; }
    }
}
