using System.Net;
using System.Net.Sockets;

namespace ErsatzTV.Core.Security;

public readonly struct IpNetwork : IEquatable<IpNetwork>
{
    public IpNetwork(IPAddress networkAddress, int prefixLength)
    {
        NetworkAddress = networkAddress ?? throw new ArgumentNullException(nameof(networkAddress));
        PrefixLength = prefixLength;
        AddressBytes = NetworkAddress.GetAddressBytes();
    }

    public IPAddress NetworkAddress { get; }

    public int PrefixLength { get; }

    private byte[] AddressBytes { get; }

    public bool Contains(IPAddress address)
    {
        if (address is null)
        {
            return false;
        }

        if (NetworkAddress.AddressFamily != address.AddressFamily)
        {
            if (NetworkAddress.AddressFamily == AddressFamily.InterNetwork &&
                address.AddressFamily == AddressFamily.InterNetworkV6 &&
                address.IsIPv4MappedToIPv6)
            {
                address = address.MapToIPv4();
            }
            else if (NetworkAddress.AddressFamily == AddressFamily.InterNetworkV6 &&
                     address.AddressFamily == AddressFamily.InterNetwork)
            {
                address = address.MapToIPv6();
            }
            else
            {
                return false;
            }
        }

        byte[] candidateBytes = address.GetAddressBytes();
        if (candidateBytes.Length != AddressBytes.Length)
        {
            return false;
        }

        int fullBytes = PrefixLength / 8;
        int remainingBits = PrefixLength % 8;

        for (int i = 0; i < fullBytes; i++)
        {
            if (candidateBytes[i] != AddressBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        int mask = (byte)~(0xFF >> remainingBits);
        return (candidateBytes[fullBytes] & mask) == (AddressBytes[fullBytes] & mask);
    }

    public static bool TryParse(string value, out IpNetwork network)
    {
        network = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string trimmed = value.Trim();
        int slashIndex = trimmed.IndexOf('/');
        if (slashIndex < 0)
        {
            return false;
        }

        string addressPart = trimmed[..slashIndex];
        string prefixPart = trimmed[(slashIndex + 1)..];
        if (!IPAddress.TryParse(addressPart, out IPAddress address) ||
            !int.TryParse(prefixPart, out int prefixLength))
        {
            return false;
        }

        int maxPrefix = address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;
        if (prefixLength < 0 || prefixLength > maxPrefix)
        {
            return false;
        }

        network = new IpNetwork(MaskNetworkAddress(address, prefixLength), prefixLength);
        return true;
    }

    public bool Equals(IpNetwork other) =>
        PrefixLength == other.PrefixLength && NetworkAddress.Equals(other.NetworkAddress);

    public override bool Equals(object obj) => obj is IpNetwork other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(NetworkAddress, PrefixLength);

    public static bool operator ==(IpNetwork left, IpNetwork right) => left.Equals(right);

    public static bool operator !=(IpNetwork left, IpNetwork right) => !left.Equals(right);

    private static IPAddress MaskNetworkAddress(IPAddress address, int prefixLength)
    {
        byte[] bytes = address.GetAddressBytes();
        int fullBytes = prefixLength / 8;
        int remainingBits = prefixLength % 8;

        for (int i = fullBytes; i < bytes.Length; i++)
        {
            bytes[i] = 0;
        }

        if (remainingBits > 0 && fullBytes < bytes.Length)
        {
            int mask = (byte)(0xFF << (8 - remainingBits));
            bytes[fullBytes] = (byte)(bytes[fullBytes] & mask);
        }

        return new IPAddress(bytes);
    }
}
