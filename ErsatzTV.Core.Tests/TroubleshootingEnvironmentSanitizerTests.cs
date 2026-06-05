using ErsatzTV.Core;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class TroubleshootingEnvironmentSanitizerTests
{
    [TestCase("ETV_ADMIN_PASSWORD", "secret123", "(redacted)")]
    [TestCase("ETV_ADMIN_USERNAME", "admin", "(redacted)")]
    [TestCase("ETV_CONFIG_FOLDER", "/config", "/config")]
    [TestCase("OIDC_CLIENT_SECRET", "abc", "(redacted)")]
    [TestCase("ETV_DISABLE_VULKAN", "1", "1")]
    public void Sanitize_RedactsSensitiveValues(string key, string value, string expected)
    {
        TroubleshootingEnvironmentSanitizer.Sanitize(key, value).ShouldBe(expected);
    }

    [Test]
    public void Sanitize_PreservesEmptySensitiveValues()
    {
        TroubleshootingEnvironmentSanitizer.Sanitize("ETV_ADMIN_PASSWORD", string.Empty).ShouldBe(string.Empty);
    }
}
