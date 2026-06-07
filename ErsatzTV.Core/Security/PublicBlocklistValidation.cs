namespace ErsatzTV.Core.Security;

public static class PublicBlocklistValidation
{
    public static Option<BaseError> ValidateCustomEntry(CustomPublicBlocklistEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            return BaseError.New("Custom blocklist name is required.");
        }

        if (entry.Name.Trim().Length > 120)
        {
            return BaseError.New("Custom blocklist name must be 120 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(entry.SourceUrl))
        {
            return BaseError.New("Custom blocklist URL is required.");
        }

        if (!Uri.TryCreate(entry.SourceUrl.Trim(), UriKind.Absolute, out Uri uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return BaseError.New("Custom blocklist URL must be a valid http or https address.");
        }

        if (entry.UpdateIntervalHours is < 1 or > 168)
        {
            return BaseError.New("Custom blocklist update interval must be between 1 and 168 hours.");
        }

        return Option<BaseError>.None;
    }

    public static Option<BaseError> ValidateCustomEntries(IReadOnlyList<CustomPublicBlocklistEntry> entries)
    {
        if (entries.Count > PublicBlocklistRegistry.MaxCustomLists)
        {
            return BaseError.New($"You can add at most {PublicBlocklistRegistry.MaxCustomLists} custom blocklists.");
        }

        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var urls = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (CustomPublicBlocklistEntry entry in entries)
        {
            Option<BaseError> single = ValidateCustomEntry(entry);
            if (single.IsSome)
            {
                return single;
            }

            string id = entry.Id?.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                return BaseError.New("Custom blocklist id is missing.");
            }

            if (!ids.Add(id))
            {
                return BaseError.New("Duplicate custom blocklist id.");
            }

            string nameKey = entry.Name.Trim();
            if (!names.Add(nameKey))
            {
                return BaseError.New($"Duplicate custom blocklist name: {nameKey}");
            }

            string urlKey = entry.SourceUrl.Trim();
            if (!urls.Add(urlKey))
            {
                return BaseError.New($"Duplicate custom blocklist URL: {urlKey}");
            }
        }

        return Option<BaseError>.None;
    }
}
