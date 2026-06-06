using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class AdminVpnBlockSettingsTests
{
    [TestCase(true, false, false, false, false, true)]
    [TestCase(false, true, false, false, false, true)]
    [TestCase(false, false, true, false, false, true)]
    [TestCase(false, false, false, false, true, true)]
    [TestCase(false, false, false, false, false, false)]
    public void ShouldBlock_DefaultMode(
        bool vpn,
        bool proxy,
        bool tor,
        bool relay,
        bool anonymous,
        bool expected)
    {
        AdminVpnBlockSettings.ShouldBlock(vpn, proxy, tor, relay, anonymous).ShouldBe(expected);
    }
}
