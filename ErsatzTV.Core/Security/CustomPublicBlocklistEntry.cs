namespace ErsatzTV.Core.Security;

public sealed class CustomPublicBlocklistEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public PublicBlocklistFormat Format { get; set; } = PublicBlocklistFormat.Mixed;

    public int UpdateIntervalHours { get; set; } = 24;

    public PublicBlocklistDefinition ToDefinition()
    {
        int hours = Math.Clamp(UpdateIntervalHours, 1, 168);
        return new PublicBlocklistDefinition(
            Id,
            Name.Trim(),
            SourceUrl.Trim(),
            "Custom",
            Format,
            Recommended: false,
            DefaultEnabled: true,
            TimeSpan.FromHours(hours),
            $"Custom list (updates every {hours}h).",
            "Custom",
            IsCustom: true);
    }
}
