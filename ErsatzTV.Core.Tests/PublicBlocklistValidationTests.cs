using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class PublicBlocklistValidationTests
{
    [Test]
    public void ValidateCustomEntry_AcceptsValidHttpUrl()
    {
        var entry = new CustomPublicBlocklistEntry
        {
            Name = "My List",
            SourceUrl = "https://example.com/list.txt",
            UpdateIntervalHours = 24
        };

        PublicBlocklistValidation.ValidateCustomEntry(entry).IsNone.ShouldBeTrue();
    }

    [Test]
    public void ValidateCustomEntry_RejectsInvalidUrl()
    {
        var entry = new CustomPublicBlocklistEntry
        {
            Name = "My List",
            SourceUrl = "ftp://example.com/list.txt"
        };

        PublicBlocklistValidation.ValidateCustomEntry(entry).IsSome.ShouldBeTrue();
    }

    [Test]
    public void ValidateCustomEntries_RejectsDuplicateUrls()
    {
        var entries = new List<CustomPublicBlocklistEntry>
        {
            new() { Id = "a", Name = "One", SourceUrl = "https://example.com/a.txt" },
            new() { Id = "b", Name = "Two", SourceUrl = "https://example.com/a.txt" }
        };

        PublicBlocklistValidation.ValidateCustomEntries(entries).IsSome.ShouldBeTrue();
    }
}
