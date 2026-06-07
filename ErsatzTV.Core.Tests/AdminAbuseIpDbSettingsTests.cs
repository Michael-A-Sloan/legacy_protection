using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class AdminAbuseIpDbSettingsTests
{
    [TestCase(100, 75, false, true)]
    [TestCase(74, 75, false, false)]
    [TestCase(75, 75, false, true)]
    [TestCase(90, 75, true, false)]
    [TestCase(90, 75, false, true)]
    public void ShouldBlock_UsesScoreThresholdAndWhitelist(
        int score,
        int minScore,
        bool isWhitelisted,
        bool expected)
    {
        AbuseIpDbLookupResult lookup = new(
            true,
            null,
            "203.0.113.10",
            score,
            isWhitelisted,
            true,
            4,
            "US",
            "United States",
            "Hosting",
            "Example ISP",
            "example.com",
            10,
            3,
            null);

        AdminAbuseIpDbSettings.ShouldBlock(lookup, minScore).ShouldBe(expected);
    }

    [Test]
    public void ShouldBlock_ReturnsFalseWhenLookupNotPerformed()
    {
        AdminAbuseIpDbSettings.ShouldBlock(AbuseIpDbLookupResult.NotConfigured(), 75).ShouldBeFalse();
    }
}
