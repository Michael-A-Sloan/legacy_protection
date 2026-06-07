using ErsatzTV.Application.Security;
using ErsatzTV.Core.Domain.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Tests.Security;

[TestFixture]
public class LoginIpAutoBanActivityRulesTests
{
    [Test]
    public void CountQualifyingFailures_ShouldCountFailedLoginsOnly_WhenAccessDeniedExcluded()
    {
        var attempts = new List<AdminLoginAttempt>
        {
            CreateAttempt(AdminLoginAttemptKind.Login, success: false),
            CreateAttempt(AdminLoginAttemptKind.Login, success: false),
            CreateAttempt(AdminLoginAttemptKind.AccessDenied, success: false),
            CreateAttempt(AdminLoginAttemptKind.Login, success: true),
            CreateAttempt(AdminLoginAttemptKind.LoginPage, success: false)
        };

        LoginIpAutoBanActivityRules.CountQualifyingFailures(attempts, includeAccessDenied: false)
            .ShouldBe(2);
    }

    [Test]
    public void CountQualifyingFailures_ShouldIncludeAccessDenied_WhenEnabled()
    {
        var attempts = new List<AdminLoginAttempt>
        {
            CreateAttempt(AdminLoginAttemptKind.Login, success: false),
            CreateAttempt(AdminLoginAttemptKind.AccessDenied, success: false),
            CreateAttempt(AdminLoginAttemptKind.AccessDenied, success: false)
        };

        LoginIpAutoBanActivityRules.CountQualifyingFailures(attempts, includeAccessDenied: true)
            .ShouldBe(3);
    }

    [Test]
    public void ShouldBanByActivity_ShouldRequireThreshold()
    {
        LoginIpAutoBanActivityRules.ShouldBanByActivity(4, 5).ShouldBeFalse();
        LoginIpAutoBanActivityRules.ShouldBanByActivity(5, 5).ShouldBeTrue();
    }

    private static AdminLoginAttempt CreateAttempt(AdminLoginAttemptKind kind, bool success) =>
        new()
        {
            AttemptKind = kind,
            Success = success,
            IpAddress = "203.0.113.10",
            Timestamp = DateTime.UtcNow
        };
}
