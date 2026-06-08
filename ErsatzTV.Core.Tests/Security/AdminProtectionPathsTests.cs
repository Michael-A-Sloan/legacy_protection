using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests.Security;

[TestFixture]
public class AdminProtectionPathsTests
{
    [Test]
    public void IsBlockedDiscoveryPath_ShouldBlockOpenApiDocsAndManifest()
    {
        AdminProtectionPaths.IsBlockedDiscoveryPath("/openapi/v1.json").ShouldBeTrue();
        AdminProtectionPaths.IsBlockedDiscoveryPath("/docs").ShouldBeTrue();
        AdminProtectionPaths.IsBlockedDiscoveryPath("/site.webmanifest").ShouldBeTrue();
        AdminProtectionPaths.IsBlockedDiscoveryPath("/login").ShouldBeFalse();
    }

    [Test]
    public void ShouldApplyPrivacyHeaders_ShouldSkipIptvAndStaticAssets()
    {
        AdminProtectionPaths.ShouldApplyPrivacyHeaders("/iptv/channels/1.m3u8").ShouldBeFalse();
        AdminProtectionPaths.ShouldApplyPrivacyHeaders("/css/site.css").ShouldBeFalse();
        AdminProtectionPaths.ShouldApplyPrivacyHeaders("/robots.txt").ShouldBeFalse();
        AdminProtectionPaths.ShouldApplyPrivacyHeaders("/settings/login-ip").ShouldBeTrue();
    }
}
