using System.Text.Json;
using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Security;

public sealed class PublicBlocklistSettings
{
    public bool MasterEnabled { get; set; } = true;

    public Dictionary<string, bool> EnabledById { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<CustomPublicBlocklistEntry> CustomLists { get; set; } = [];

    public static async Task<PublicBlocklistSettings> LoadAsync(
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken)
    {
        Option<string> stored = await configElementRepository
            .GetValue<string>(ConfigElementKey.AdminLoginIpPublicBlocklists, cancellationToken);

        PublicBlocklistSettings settings = stored.Match(
            json =>
            {
                try
                {
                    return JsonSerializer.Deserialize<PublicBlocklistSettings>(json) ?? CreateDefaults();
                }
                catch (JsonException)
                {
                    return CreateDefaults();
                }
            },
            CreateDefaults);

        settings.CustomLists ??= [];

        foreach (PublicBlocklistDefinition definition in PublicBlocklistCatalog.All)
        {
            if (!settings.EnabledById.ContainsKey(definition.Id))
            {
                settings.EnabledById[definition.Id] = definition.DefaultEnabled;
            }
        }

        foreach (CustomPublicBlocklistEntry custom in settings.CustomLists)
        {
            if (!settings.EnabledById.ContainsKey(custom.Id))
            {
                settings.EnabledById[custom.Id] = true;
            }
        }

        return settings;
    }

    public static async Task SaveAsync(
        IConfigElementRepository configElementRepository,
        PublicBlocklistSettings settings,
        CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(settings);
        await configElementRepository.Upsert(
            ConfigElementKey.AdminLoginIpPublicBlocklists,
            json,
            cancellationToken);
    }

    public bool IsListEnabled(PublicBlocklistDefinition definition) =>
        EnabledById.TryGetValue(definition.Id, out bool enabled) ? enabled : definition.DefaultEnabled;

    public IReadOnlyList<PublicBlocklistDefinition> GetAllDefinitions() =>
        PublicBlocklistRegistry.GetAllDefinitions(this);

    private static PublicBlocklistSettings CreateDefaults()
    {
        var settings = new PublicBlocklistSettings();
        foreach (PublicBlocklistDefinition definition in PublicBlocklistCatalog.All)
        {
            settings.EnabledById[definition.Id] = definition.DefaultEnabled;
        }

        return settings;
    }
}
