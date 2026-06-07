using ErsatzTV.Core.Domain.Security;

namespace ErsatzTV.Application.Security;

public static class LoginIpAutoBanActivityRules
{
    public static int CountQualifyingFailures(
        IEnumerable<AdminLoginAttempt> attempts,
        bool includeAccessDenied)
    {
        int count = 0;

        foreach (AdminLoginAttempt attempt in attempts)
        {
            switch (attempt.AttemptKind)
            {
                case AdminLoginAttemptKind.Login when !attempt.Success:
                    count++;
                    break;
                case AdminLoginAttemptKind.AccessDenied when includeAccessDenied:
                    count++;
                    break;
            }
        }

        return count;
    }

    public static bool ShouldBanByActivity(int failureCount, int minFailedAttempts) =>
        failureCount >= minFailedAttempts;
}
