namespace ErsatzTV.Core;

public static class AdminSecurityLogMessages
{
    public const string Marker = "[AdminSecurity] ";

    public static bool IsAdminSecurityLogMessage(string message) =>
        message.Contains(Marker, StringComparison.Ordinal);
}
