using ErsatzTV.Core;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class AppDataFolderResolverTests
{
    [Test]
    public void Resolve_UsesCustomFolderWhenDatabaseExistsThere()
    {
        string root = CreateTempRoot();
        string custom = Path.Combine(root, "config");
        string legacy = Path.Combine(root, "legacy");
        Directory.CreateDirectory(custom);
        File.WriteAllText(Path.Combine(custom, AppDataFolderResolver.DatabaseFileName), "db");

        AppDataFolderResolver.Resolve(legacy, custom, _ => false).ShouldBe(custom);
    }

    [Test]
    public void Resolve_UsesLegacyFolderWhenOnlyLegacyHasDatabase()
    {
        string root = CreateTempRoot();
        string custom = Path.Combine(root, "config");
        string legacy = Path.Combine(root, "legacy");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, AppDataFolderResolver.DatabaseFileName), "db");

        AppDataFolderResolver.Resolve(legacy, custom, _ => false).ShouldBe(legacy);
    }

    [Test]
    public void Resolve_UsesCustomFolderForFreshInstall()
    {
        string root = CreateTempRoot();
        string custom = Path.Combine(root, "config");
        string legacy = Path.Combine(root, "legacy");

        AppDataFolderResolver.Resolve(legacy, custom, _ => false).ShouldBe(custom);
    }

    [Test]
    public void Resolve_UsesDefaultWhenCustomFolderNotConfigured()
    {
        string root = CreateTempRoot();
        string legacy = Path.Combine(root, "legacy");

        AppDataFolderResolver.Resolve(legacy, null, _ => false).ShouldBe(legacy);
    }

    [Test]
    public void Resolve_UsesMountedLegacyFolderForFreshInstall()
    {
        string root = CreateTempRoot();
        string custom = Path.Combine(root, "config");
        string legacy = Path.Combine(root, "legacy");
        Directory.CreateDirectory(legacy);

        AppDataFolderResolver.Resolve(legacy, custom, path => path == legacy).ShouldBe(legacy);
    }

    [Test]
    public void Resolve_CopiesDatabaseToMountedCustomFolder()
    {
        string root = CreateTempRoot();
        string custom = Path.Combine(root, "config");
        string legacy = Path.Combine(root, "legacy");
        Directory.CreateDirectory(legacy);
        Directory.CreateDirectory(custom);
        File.WriteAllText(Path.Combine(legacy, AppDataFolderResolver.DatabaseFileName), "db");

        string resolved = AppDataFolderResolver.Resolve(legacy, custom, path => path == custom);

        resolved.ShouldBe(custom);
        File.Exists(Path.Combine(custom, AppDataFolderResolver.DatabaseFileName)).ShouldBeTrue();
    }

    private static string CreateTempRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "etv-config-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }
}
