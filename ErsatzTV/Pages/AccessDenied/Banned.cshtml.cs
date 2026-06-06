using ErsatzTV.Core.Networking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ErsatzTV.Pages.AccessDenied;

[AllowAnonymous]
public class BannedModel : PageModel
{
    public string IpDisplay { get; private set; }

    public IActionResult OnGet()
    {
        if (!AdminAuthHelper.IsEnabled)
        {
            return Redirect("/");
        }

        IpAddressPair clientIp = ClientIpHelper.GetClientIpInfo(HttpContext);
        IpDisplay = clientIp.Display;
        return Page();
    }
}
