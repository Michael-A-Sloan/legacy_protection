using System.Net;
using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class PublicBlocklistParserTests
{
    [Test]
    public void Parse_IpAndCidrEntries()
    {
        PublicBlocklistParseResult result = PublicBlocklistParser.Parse(
        [
            "# comment",
            "203.0.113.10",
            "203.0.113.20\t5",
            "198.51.100.0/24"
        ]);

        result.IpAddresses.ShouldBe(["203.0.113.10", "203.0.113.20"]);
        result.Networks.Count.ShouldBe(1);
        result.Networks[0].Contains(IPAddress.Parse("198.51.100.42")).ShouldBeTrue();
    }
}

[TestFixture]
public class IpNetworkTests
{
    [TestCase("10.0.0.5", "10.0.0.0/8", true)]
    [TestCase("10.0.0.5", "192.168.0.0/16", false)]
    public void Contains_MatchesCidr(string ip, string cidr, bool expected)
    {
        IpNetwork.TryParse(cidr, out IpNetwork network).ShouldBeTrue();
        network.Contains(IPAddress.Parse(ip)).ShouldBe(expected);
    }
}
