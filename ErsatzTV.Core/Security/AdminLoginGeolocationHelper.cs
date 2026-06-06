namespace ErsatzTV.Core.Security;

public static class AdminLoginGeolocationHelper
{
    public static bool TryValidate(double? latitude, double? longitude, out string errorMessage)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            errorMessage = "Browser location is required to sign in.";
            return false;
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            errorMessage = "Invalid browser location coordinates.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    public static string FormatDisplay(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return string.Empty;
        }

        return $"{latitude.Value:F5}, {longitude.Value:F5}";
    }

    public static string BuildMapsUrl(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"https://www.google.com/maps?q={latitude.Value},{longitude.Value}");
    }
}
