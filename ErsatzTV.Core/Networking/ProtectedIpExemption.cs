using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ErsatzTV.Core.Networking;

public static class ProtectedIpExemption
{
    private static readonly object SyncRoot = new();
    private static System.Collections.Generic.HashSet<string> _protectedAddresses = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _loadedAtUtc = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public static bool IsExempt(string ipAddress)
    {
        if (LocalIpExemption.IsExempt(ipAddress))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return false;
        }

        return IPAddress.TryParse(ipAddress.Trim(), out IPAddress parsed) && IsExempt(parsed);
    }

    public static bool IsExempt(IPAddress address)
    {
        if (LocalIpExemption.IsExempt(address))
        {
            return true;
        }

        if (address is null)
        {
            return false;
        }

        EnsureProtectedAddressesLoaded();
        return _protectedAddresses.Contains(NormalizeAddress(address));
    }

    public static bool IsExempt(IpAddressPair clientIp) =>
        IsExempt(clientIp.Ipv4) ||
        IsExempt(clientIp.Ipv6) ||
        IsExempt(clientIp.Canonical);

    private static void EnsureProtectedAddressesLoaded()
    {
        if (DateTime.UtcNow - _loadedAtUtc < CacheDuration)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (DateTime.UtcNow - _loadedAtUtc < CacheDuration)
            {
                return;
            }

            _protectedAddresses = LoadProtectedAddresses();
            _loadedAtUtc = DateTime.UtcNow;
        }
    }

    private static System.Collections.Generic.HashSet<string> LoadProtectedAddresses()
    {
        var addresses = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation address in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    AddAddress(addresses, address.Address);
                }
            }
        }
        catch
        {
            // Best effort only; localhost protection still applies.
        }

        AddBaseUrlAddresses(addresses);

        return addresses;
    }

    private static void AddBaseUrlAddresses(System.Collections.Generic.HashSet<string> addresses)
    {
        string baseUrl = SystemEnvironment.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        try
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri))
            {
                return;
            }

            if (IPAddress.TryParse(uri.Host, out IPAddress literal))
            {
                AddAddress(addresses, literal);
                return;
            }

            foreach (IPAddress resolved in Dns.GetHostAddresses(uri.Host))
            {
                AddAddress(addresses, resolved);
            }
        }
        catch
        {
            // Ignore DNS failures for optional base URL protection.
        }
    }

    private static void AddAddress(System.Collections.Generic.HashSet<string> addresses, IPAddress address)
    {
        if (address is null || LocalIpExemption.IsExempt(address))
        {
            return;
        }

        addresses.Add(NormalizeAddress(address));

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            addresses.Add(NormalizeAddress(address.MapToIPv6()));
        }

        if (address.IsIPv4MappedToIPv6)
        {
            addresses.Add(NormalizeAddress(address.MapToIPv4()));
        }
    }

    private static string NormalizeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }
}
