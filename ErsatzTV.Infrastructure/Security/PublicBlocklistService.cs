using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using Serilog;

namespace ErsatzTV.Infrastructure.Security;

public sealed class PublicBlocklistService(
    IHttpClientFactory httpClientFactory,
    IConfigElementRepository configElementRepository,
    PublicBlocklistStore store) : IPublicBlocklistService
{
    public IReadOnlyList<PublicBlocklistDefinition> GetDefinitions()
    {
        PublicBlocklistSettings settings = LoadSettings();
        return settings.GetAllDefinitions();
    }

    public IReadOnlyList<PublicBlocklistStatus> GetStatuses()
    {
        PublicBlocklistSettings settings = LoadSettings();
        return settings.GetAllDefinitions()
            .Select(definition => store.BuildStatus(definition, settings.IsListEnabled(definition)))
            .ToList();
    }

    public async Task<PublicBlocklistMatchResult> MatchClientIpAsync(
        IpAddressPair clientIp,
        CancellationToken cancellationToken)
    {
        PublicBlocklistSettings settings = await PublicBlocklistSettings.LoadAsync(configElementRepository, cancellationToken);
        if (!settings.MasterEnabled)
        {
            return new PublicBlocklistMatchResult(false, string.Empty, string.Empty);
        }

        foreach (PublicBlocklistDefinition definition in settings.GetAllDefinitions())
        {
            if (!settings.IsListEnabled(definition))
            {
                continue;
            }

            if (store.TryMatch(definition.Id, clientIp, out _))
            {
                return new PublicBlocklistMatchResult(true, definition.Id, definition.Name);
            }
        }

        return new PublicBlocklistMatchResult(false, string.Empty, string.Empty);
    }

    public async Task RefreshListAsync(string listId, CancellationToken cancellationToken)
    {
        PublicBlocklistSettings settings = await PublicBlocklistSettings.LoadAsync(configElementRepository, cancellationToken);
        PublicBlocklistDefinition definition = PublicBlocklistRegistry.FindDefinition(settings, listId);
        if (definition is null)
        {
            return;
        }

        store.SetUpdating(definition.Id);

        try
        {
            HttpClient client = httpClientFactory.CreateClient("PublicBlocklists");
            using HttpResponseMessage response = await client.GetAsync(definition.SourceUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string message = $"Download failed ({(int)response.StatusCode}).";
                store.SetSnapshot(definition.Id, new PublicBlocklistParseResult([], []), message);
                Log.Logger.Warning(
                    "Public blocklist {ListId} download failed with status {StatusCode}",
                    definition.Id,
                    (int)response.StatusCode);
                return;
            }

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            PublicBlocklistParseResult parsed = PublicBlocklistParser.Parse(content.Split('\n'));
            store.SetSnapshot(definition.Id, parsed, null);

            Log.Logger.Information(
                "Updated public blocklist {ListName}: {IpCount} IPs, {NetworkCount} networks",
                definition.Name,
                parsed.IpAddresses.Count,
                parsed.Networks.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            store.SetSnapshot(definition.Id, new PublicBlocklistParseResult([], []), ex.Message);
            Log.Logger.Warning(ex, "Public blocklist {ListId} update failed", definition.Id);
        }
    }

    public async Task RefreshDueListsAsync(CancellationToken cancellationToken)
    {
        PublicBlocklistSettings settings = await PublicBlocklistSettings.LoadAsync(configElementRepository, cancellationToken);
        IReadOnlyList<PublicBlocklistStatus> statuses = GetStatuses();

        foreach (PublicBlocklistDefinition definition in settings.GetAllDefinitions())
        {
            PublicBlocklistStatus status = statuses.First(s => s.Id == definition.Id);
            bool due = !status.LastUpdatedUtc.HasValue ||
                       DateTimeOffset.UtcNow - status.LastUpdatedUtc.Value >= definition.UpdateInterval;

            if (due && !status.IsUpdating)
            {
                await RefreshListAsync(definition.Id, cancellationToken);
            }
        }
    }

    private PublicBlocklistSettings LoadSettings() =>
        PublicBlocklistSettings.LoadAsync(configElementRepository, CancellationToken.None).GetAwaiter().GetResult();
}
