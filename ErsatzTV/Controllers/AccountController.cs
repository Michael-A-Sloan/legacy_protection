using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErsatzTV.Controllers;

[ApiController]
public class AccountController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("account/logout")]
    public IActionResult Logout()
    {
        if (OidcHelper.IsEnabled)
        {
            return new SignOutResult(["oidc", "cookie"]);
        }

        if (AdminAuthHelper.IsEnabled)
        {
            return new SignOutResult(["cookie"]);
        }

        return Ok();
    }
}
