using Serilog;

namespace ErsatzTV.Core;

public static class AppDataFolderResolver
{
    public const string DatabaseFileName = "ersatztv.sqlite3";

    public static string Resolve(string defaultConfigFolder, string customConfigFolder) =>
        Resolve(defaultConfigFolder, customConfigFolder, LinuxMountHelper.IsMountPoint);

    public static string Resolve(
        string defaultConfigFolder,
        string customConfigFolder,
        Func<string, bool> isMountPoint)
    {
        if (string.IsNullOrWhiteSpace(customConfigFolder))
        {
            return defaultConfigFolder;
        }

        string customDatabasePath = Path.Combine(customConfigFolder, DatabaseFileName);
        string defaultDatabasePath = Path.Combine(defaultConfigFolder, DatabaseFileName);

        bool customMounted = isMountPoint(customConfigFolder);
        bool defaultMounted = isMountPoint(defaultConfigFolder);

        if (customMounted && !defaultMounted)
        {
            return UseMountedFolder(customConfigFolder, defaultConfigFolder, customDatabasePath, defaultDatabasePath);
        }

        if (defaultMounted && !customMounted)
        {
            return UseMountedFolder(defaultConfigFolder, customConfigFolder, defaultDatabasePath, customDatabasePath);
        }

        if (customMounted && defaultMounted)
        {
            if (File.Exists(customDatabasePath))
            {
                return customConfigFolder;
            }

            if (File.Exists(defaultDatabasePath))
            {
                return defaultConfigFolder;
            }

            return customConfigFolder;
        }

        if (File.Exists("/.dockerenv"))
        {
            Log.Logger.Warning(
                "No host volume detected at {Custom} or {Legacy}. " +
                "Map /mnt/user/appdata/ErsatzTV on Unraid to container path {Custom} or {Legacy}.",
                customConfigFolder,
                defaultConfigFolder,
                customConfigFolder,
                defaultConfigFolder);
        }

        if (File.Exists(customDatabasePath))
        {
            return customConfigFolder;
        }

        if (File.Exists(defaultDatabasePath))
        {
            Log.Logger.Warning(
                "Using legacy config path {Legacy} because it contains {Database}. " +
                "Map your host appdata folder to container path {Custom} or {Legacy}.",
                defaultConfigFolder,
                DatabaseFileName,
                customConfigFolder,
                defaultConfigFolder);

            return defaultConfigFolder;
        }

        return customConfigFolder;
    }

    private static string UseMountedFolder(
        string mountedFolder,
        string otherFolder,
        string mountedDatabasePath,
        string otherDatabasePath)
    {
        if (File.Exists(mountedDatabasePath))
        {
            Log.Logger.Information("Using mounted config path {Folder}", mountedFolder);
            return mountedFolder;
        }

        if (File.Exists(otherDatabasePath))
        {
            Log.Logger.Information(
                "Copying config data from {Source} to mounted path {Target}",
                otherFolder,
                mountedFolder);

            TryCopyConfigFolder(otherFolder, mountedFolder);
        }
        else
        {
            Log.Logger.Information("Using mounted config path {Folder} for new install", mountedFolder);
        }

        return mountedFolder;
    }

    private static void TryCopyConfigFolder(string sourceFolder, string targetFolder)
    {
        try
        {
            Directory.CreateDirectory(targetFolder);

            foreach (string sourceFile in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceFolder, sourceFile);
                string targetFile = Path.Combine(targetFolder, relativePath);
                string targetDirectory = Path.GetDirectoryName(targetFile)!;

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                if (!File.Exists(targetFile))
                {
                    File.Copy(sourceFile, targetFile);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(
                ex,
                "Failed to copy config data from {Source} to {Target}",
                sourceFolder,
                targetFolder);
        }
    }
}
