namespace ErsatzTV.Core.Security;

public sealed record PublicBlocklistDefinition(
    string Id,
    string Name,
    string SourceUrl,
    string SourceLabel,
    PublicBlocklistFormat Format,
    bool Recommended,
    bool DefaultEnabled,
    TimeSpan UpdateInterval,
    string Description,
    string Category,
    bool IsCustom = false);
