using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Core.Interfaces.Security;

public record PublicBlocklistMatchResult(
    bool IsBlocked,
    string ListId,
    string ListName);

public record PublicBlocklistStatus(
    string Id,
    string Name,
    bool Recommended,
    bool Enabled,
    int EntryCount,
    int NetworkCount,
    DateTimeOffset? LastUpdatedUtc,
    DateTimeOffset? NextUpdateUtc,
    string LastError,
    bool IsUpdating);

public interface IPublicBlocklistService
{
    IReadOnlyList<PublicBlocklistDefinition> GetDefinitions();

    IReadOnlyList<PublicBlocklistStatus> GetStatuses();

    Task<PublicBlocklistMatchResult> MatchClientIpAsync(
        IpAddressPair clientIp,
        CancellationToken cancellationToken);

    Task RefreshListAsync(string listId, CancellationToken cancellationToken);

    Task RefreshDueListsAsync(CancellationToken cancellationToken);
}
