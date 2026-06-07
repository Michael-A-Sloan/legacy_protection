namespace ErsatzTV.Core.Security;

public static class PublicBlocklistRegistry
{
    public const int MaxCustomLists = 32;

    public static IReadOnlyList<PublicBlocklistDefinition> GetAllDefinitions(PublicBlocklistSettings settings) =>
        PublicBlocklistCatalog.All
            .Concat(settings.CustomLists.Select(entry => entry.ToDefinition()))
            .ToList();

    public static PublicBlocklistDefinition FindDefinition(PublicBlocklistSettings settings, string id) =>
        GetAllDefinitions(settings)
            .FirstOrDefault(definition =>
                string.Equals(definition.Id, id, StringComparison.OrdinalIgnoreCase));
}
