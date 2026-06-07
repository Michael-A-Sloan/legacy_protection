using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;

namespace ErsatzTV.Infrastructure.Security;

public sealed class NullPublicBlocklistService : IPublicBlocklistService
{
    public IReadOnlyList<PublicBlocklistDefinition> GetDefinitions() => PublicBlocklistCatalog.All;

    public IReadOnlyList<PublicBlocklistStatus> GetStatuses() =>
        PublicBlocklistCatalog.All
            .Select(definition => new PublicBlocklistStatus(
                definition.Id,
                definition.Name,
                definition.Recommended,
                definition.DefaultEnabled,
                0,
                0,
                null,
                null,
                string.Empty,
                false))
            .ToList();

    public Task<PublicBlocklistMatchResult> MatchClientIpAsync(
        IpAddressPair clientIp,
        CancellationToken cancellationToken) =>
        Task.FromResult(new PublicBlocklistMatchResult(false, string.Empty, string.Empty));

    public Task RefreshListAsync(string listId, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RefreshDueListsAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
