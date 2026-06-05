namespace ErsatzTV.Core;

public static class LinuxMountHelper
{
    public static bool IsMountPoint(string path)
    {
        if (!OperatingSystem.IsLinux() || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string normalized = NormalizeMountPath(path);

        try
        {
            foreach (string mountPoint in GetMountPoints())
            {
                if (string.Equals(mountPoint, normalized, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    internal static System.Collections.Generic.HashSet<string> GetMountPoints()
    {
        var mountPoints = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        foreach (string line in File.ReadAllLines("/proc/self/mountinfo"))
        {
            int separator = line.IndexOf(" - ", StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            string[] parts = line[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5)
            {
                continue;
            }

            string mountPoint = UnescapeMountPath(parts[4]);
            mountPoints.Add(NormalizeMountPath(mountPoint));
        }

        return mountPoints;
    }

    private static string NormalizeMountPath(string path)
    {
        string normalized = Path.GetFullPath(path).TrimEnd('/');
        return normalized.Length == 0 ? "/" : normalized;
    }

    private static string UnescapeMountPath(string path) => path.Replace("\\040", " ", StringComparison.Ordinal);
}
