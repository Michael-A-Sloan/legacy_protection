using ErsatzTV.Core.Security;

namespace ErsatzTV.Application.Security;

public class PublicBlocklistItemViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string SourceLabel { get; set; }
    public string SourceUrl { get; set; }
    public PublicBlocklistFormat Format { get; set; }
    public int UpdateIntervalHours { get; set; }
    public bool Recommended { get; set; }
    public bool IsCustom { get; set; }
    public bool Enabled { get; set; }
    public int EntryCount { get; set; }
    public int NetworkCount { get; set; }
    public DateTimeOffset? LastUpdatedUtc { get; set; }
    public DateTimeOffset? NextUpdateUtc { get; set; }
    public string LastError { get; set; }
    public bool IsUpdating { get; set; }
}
