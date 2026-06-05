using ErsatzTV.Core;
using Serilog;

namespace ErsatzTV;

public static class AdminSecurityLog
{
    public static void Information(string messageTemplate, params object[] propertyValues) =>
        Log.Information(AdminSecurityLogMessages.Marker + messageTemplate, propertyValues);

    public static void Warning(string messageTemplate, params object[] propertyValues) =>
        Log.Warning(AdminSecurityLogMessages.Marker + messageTemplate, propertyValues);
}
