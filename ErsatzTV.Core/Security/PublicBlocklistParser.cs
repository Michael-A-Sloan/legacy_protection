namespace ErsatzTV.Core.Security;

public static class PublicBlocklistParser
{
    public static PublicBlocklistParseResult Parse(IEnumerable<string> lines)
    {
        var ips = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var networks = new List<IpNetwork>();

        foreach (string rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                continue;
            }

            string line = rawLine.Trim();
            if (line.StartsWith('#'))
            {
                continue;
            }

            string token = line.Split('\t', ' ', StringSplitOptions.RemoveEmptyEntries)[0];
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (token.Contains('/', StringComparison.Ordinal))
            {
                if (IpNetwork.TryParse(token, out IpNetwork network))
                {
                    networks.Add(network);
                }

                continue;
            }

            if (System.Net.IPAddress.TryParse(token, out System.Net.IPAddress address))
            {
                ips.Add(address.ToString());
            }
        }

        return new PublicBlocklistParseResult(ips, networks);
    }
}

public sealed record PublicBlocklistParseResult(
    System.Collections.Generic.HashSet<string> IpAddresses,
    List<IpNetwork> Networks);
