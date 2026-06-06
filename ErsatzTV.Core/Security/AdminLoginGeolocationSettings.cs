using ErsatzTV.Core.Domain;
using ErsatzTV.Core.Interfaces.Repositories;

namespace ErsatzTV.Core.Security;

public static class AdminLoginGeolocationSettings
{
    public static bool IsRequiredFromEnvironment
    {
        get
        {
            string value = Environment.GetEnvironmentVariable("ETV_LOGIN_GEOLOCATION_REQUIRED");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static async Task<bool> IsRequiredAsync(
        IConfigElementRepository configElementRepository,
        CancellationToken cancellationToken)
    {
        if (IsRequiredFromEnvironment)
        {
            return true;
        }

        return await configElementRepository
            .GetValue<bool>(ConfigElementKey.AdminLoginGeolocationRequired, cancellationToken)
            .IfNoneAsync(false);
    }
}
