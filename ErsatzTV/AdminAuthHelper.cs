using System.Security.Cryptography;
using System.Text;

namespace ErsatzTV;

public static class AdminAuthHelper
{
    public static string Username { get; private set; } = "admin";
    public static string Password { get; private set; }
    public static bool IsEnabled { get; private set; }

    public static void Init(IConfiguration configuration)
    {
        Username = FirstNonEmpty(
            Environment.GetEnvironmentVariable("ETV_ADMIN_USERNAME"),
            configuration["ADMIN:Username"],
            "admin");

        Password = FirstNonEmpty(
            Environment.GetEnvironmentVariable("ETV_ADMIN_PASSWORD"),
            configuration["ADMIN:Password"]);

        IsEnabled = !string.IsNullOrWhiteSpace(Password) && !OidcHelper.IsEnabled;
    }

    public static bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        if (!string.Equals(username, Username, StringComparison.Ordinal))
        {
            return false;
        }

        byte[] provided = Encoding.UTF8.GetBytes(password);
        byte[] expected = Encoding.UTF8.GetBytes(Password);

        return provided.Length == expected.Length &&
               CryptographicOperations.FixedTimeEquals(provided, expected);
    }

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
