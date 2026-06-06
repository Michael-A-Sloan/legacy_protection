using ErsatzTV.Core.Security;
using NUnit.Framework;
using Shouldly;

namespace ErsatzTV.Core.Tests;

[TestFixture]
public class AdminLoginGeolocationHelperTests
{
    [Test]
    public void TryValidate_RejectsMissingCoordinates()
    {
        AdminLoginGeolocationHelper.TryValidate(null, null, out string error).ShouldBeFalse();
        error.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public void TryValidate_AcceptsValidCoordinates()
    {
        AdminLoginGeolocationHelper.TryValidate(40.7128, -74.0060, out string error).ShouldBeTrue();
        error.ShouldBeNull();
    }

    [Test]
    public void BuildMapsUrl_FormatsGoogleMapsLink()
    {
        AdminLoginGeolocationHelper.BuildMapsUrl(40.7128, -74.0060)
            .ShouldBe("https://www.google.com/maps?q=40.7128,-74.006");
    }
}
