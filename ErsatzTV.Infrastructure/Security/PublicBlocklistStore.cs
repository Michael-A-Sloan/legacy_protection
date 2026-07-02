using System.Collections.Concurrent;
using System.Net;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Infrastructure.Security;

public sealed class PublicBlocklistStore
{
    private readonly ConcurrentDictionary<string, ListSnapshot> _lists = new(StringComparer.OrdinalIgnoreCase);

    public void SetSnapshot(string listId, PublicBlocklistParseResult parsed, string errorMessage)
    {
        _lists[listId] = new ListSnapshot(
            parsed.IpAddresses,
            parsed.Networks,
            DateTimeOffset.UtcNow,
            errorMessage ?? string.Empty,
            false);
    }

    public void SetUpdating(string listId)
    {
        _lists.AddOrUpdate(
            listId,
            _ => new ListSnapshot([], [], null, string.Empty, true),
            (_, existing) => existing with { IsUpdating = true });
    }

    public void ClearUpdating(string listId)
    {
        _lists.AddOrUpdate(
            listId,
            _ => new ListSnapshot([], [], null, string.Empty, false),
            (_, existing) => existing with { IsUpdating = false });
    }

    public bool TryMatch(string listId, IpAddressPair clientIp, out string matchedAddress)
    {
        matchedAddress = string.Empty;
        if (!_lists.TryGetValue(listId, out ListSnapshot snapshot))
        {
            return false;
        }

        foreach (string candidate in EnumerateCandidates(clientIp))
        {
            if (snapshot.IpAddresses.Contains(candidate))
            {
                matchedAddress = candidate;
                return true;
            }

            if (!IPAddress.TryParse(candidate, out IPAddress parsed))
            {
                continue;
            }

            foreach (IpNetwork network in snapshot.Networks)
            {
                if (network.Contains(parsed))
                {
                    matchedAddress = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    public PublicBlocklistStatus BuildStatus(
        PublicBlocklistDefinition definition,
        bool enabled)
    {
        _lists.TryGetValue(definition.Id, out ListSnapshot snapshot);
        snapshot ??= new ListSnapshot([], [], null, string.Empty, false);

        DateTimeOffset? lastUpdated = snapshot.LastUpdatedUtc;
        DateTimeOffset? nextUpdate = lastUpdated?.Add(definition.UpdateInterval);

        return new PublicBlocklistStatus(
            definition.Id,
            definition.Name,
            definition.Recommended,
            enabled,
            snapshot.IpAddresses.Count,
            snapshot.Networks.Count,
            lastUpdated,
            nextUpdate,
            snapshot.LastError,
            snapshot.IsUpdating);
    }

    private static IEnumerable<string> EnumerateCandidates(IpAddressPair clientIp)
    {
        if (!string.IsNullOrWhiteSpace(clientIp.Ipv4))
        {
            yield return clientIp.Ipv4;
        }

        if (!string.IsNullOrWhiteSpace(clientIp.Ipv6))
        {
            yield return clientIp.Ipv6;
        }

        if (!string.IsNullOrWhiteSpace(clientIp.Canonical))
        {
            yield return clientIp.Canonical;
        }
    }

    private sealed record ListSnapshot(
        System.Collections.Generic.HashSet<string> IpAddresses,
        List<IpNetwork> Networks,
        DateTimeOffset? LastUpdatedUtc,
        string LastError,
        bool IsUpdating);
}
