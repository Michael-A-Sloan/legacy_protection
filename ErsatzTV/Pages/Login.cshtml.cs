using System.Security.Claims;
using ErsatzTV.Core.Interfaces.Repositories;
using ErsatzTV.Core.Interfaces.Security;
using ErsatzTV.Core.Networking;
using ErsatzTV.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ErsatzTV.Pages;

[AllowAnonymous]
public class LoginModel(
    IAdminLoginProtectionService loginProtectionService,
    IConfigElementRepository configElementRepository) : PageModel
{
    [BindProperty]
    public string Username { get; set; }

    [BindProperty]
    public string Password { get; set; }

    [BindProperty(SupportsGet = true)]
    public string ReturnUrl { get; set; }

    [BindProperty]
    public double? Latitude { get; set; }

    [BindProperty]
    public double? Longitude { get; set; }

    [BindProperty]
    public double? LocationAccuracyMeters { get; set; }

    public string ErrorMessage { get; private set; }

    public bool RequireGeolocation { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!AdminAuthHelper.IsEnabled)
        {
            return Redirect("/");
        }

        RequireGeolocation =
            await AdminLoginGeolocationSettings.IsRequiredAsync(configElementRepository, cancellationToken);

        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectSafeReturnUrl(ReturnUrl);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!AdminAuthHelper.IsEnabled)
        {
            return Redirect("/");
        }

        RequireGeolocation =
            await AdminLoginGeolocationSettings.IsRequiredAsync(configElementRepository, cancellationToken);

        IpAddressPair clientIp = ClientIpHelper.GetClientIpInfo(HttpContext);
        string userAgent = Request.Headers.UserAgent.ToString();

        if (RequireGeolocation && !AdminLoginGeolocationHelper.TryValidate(Latitude, Longitude, out string geoError))
        {
            await loginProtectionService.RecordAttemptAsync(
                clientIp,
                Username,
                false,
                userAgent,
                geoError,
                latitude: Latitude,
                longitude: Longitude,
                locationAccuracyMeters: LocationAccuracyMeters,
                cancellationToken: cancellationToken);

            ErrorMessage = geoError;
            return Page();
        }

        AdminLoginAccessResult accessResult =
            await loginProtectionService.CheckAccessAsync(clientIp, cancellationToken);

        if (!accessResult.Allowed)
        {
            await loginProtectionService.RecordAttemptAsync(
                clientIp,
                Username,
                false,
                userAgent,
                accessResult.DenyReason,
                latitude: Latitude,
                longitude: Longitude,
                locationAccuracyMeters: LocationAccuracyMeters,
                cancellationToken: cancellationToken);

            AdminSecurityLog.Warning(
                "Admin login blocked for {Username} from {RemoteIP}: {Reason}",
                Username ?? "(empty)",
                clientIp.Display,
                accessResult.DenyReason);

            ErrorMessage = accessResult.DenyReason;
            return Page();
        }

        if (AdminAuthHelper.ValidateCredentials(Username, Password))
        {
            var claims = new[] { new Claim(ClaimTypes.Name, Username) };
            var identity = new ClaimsIdentity(claims, "cookie");
            await HttpContext.SignInAsync("cookie", new ClaimsPrincipal(identity));

            await loginProtectionService.RecordAttemptAsync(
                clientIp,
                Username,
                true,
                userAgent,
                null,
                latitude: Latitude,
                longitude: Longitude,
                locationAccuracyMeters: LocationAccuracyMeters,
                cancellationToken: cancellationToken);

            AdminSecurityLog.Information(
                "Admin login succeeded for {Username} from {RemoteIP} at {Location}",
                Username,
                clientIp.Display,
                AdminLoginGeolocationHelper.FormatDisplay(Latitude, Longitude));

            return RedirectSafeReturnUrl(ReturnUrl);
        }

        await loginProtectionService.RecordAttemptAsync(
            clientIp,
            Username,
            false,
            userAgent,
            "Invalid username or password.",
            latitude: Latitude,
            longitude: Longitude,
            locationAccuracyMeters: LocationAccuracyMeters,
            cancellationToken: cancellationToken);

        AdminSecurityLog.Warning(
            "Admin login failed for {Username} from {RemoteIP}",
            Username ?? "(empty)",
            clientIp.Display);

        ErrorMessage = "Invalid username or password.";
        return Page();
    }

    private IActionResult RedirectSafeReturnUrl(string returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl) &&
            !returnUrl.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return Redirect("/");
    }
}
