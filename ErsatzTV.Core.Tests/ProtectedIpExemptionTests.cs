using System.Net;
using System.Net.NetworkInformation;
using ErsatzTV.Core.Networking;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class ProtectedIpExemptionTests
{
    [TestCase("127.0.0.1")]
    [TestCase("::1")]
    [TestCase("0.0.0.0")]
    public void IsExempt_ProtectsLocalAddresses(string ipAddress)
    {
        ProtectedIpExemption.IsExempt(ipAddress).ShouldBeTrue();
    }

    [Test]
    public void IsExempt_ProtectsLocalNetworkInterfaceAddresses()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (UnicastIPAddressInformation address in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ProtectedIpExemption.IsExempt(address.Address.ToString()).ShouldBeTrue();
                }
            }
        }
    }

    [Test]
    public void IsExempt_DoesNotProtectPublicAddresses()
    {
        ProtectedIpExemption.IsExempt("8.8.8.8").ShouldBeFalse();
    }
}
