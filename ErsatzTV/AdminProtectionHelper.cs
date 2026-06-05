namespace ErsatzTV;

public static class AdminProtectionHelper
{
    public static bool IsEnabled => OidcHelper.IsEnabled || AdminAuthHelper.IsEnabled;
}
