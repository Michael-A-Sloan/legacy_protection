using Serilog;

namespace ErsatzTV.Core;

public static class AppDataFolderResolver
{
    public const string DatabaseFileName = "ersatztv.sqlite3";

    public static string Resolve(string defaultConfigFolder, string customConfigFolder)
    {
        if (string.IsNullOrWhiteSpace(customConfigFolder))
        {
            return defaultConfigFolder;
        }

        string customDatabasePath = Path.Combine(customConfigFolder, DatabaseFileName);
        string defaultDatabasePath = Path.Combine(defaultConfigFolder, DatabaseFileName);

        if (File.Exists(customDatabasePath))
        {
            return customConfigFolder;
        }

        if (File.Exists(defaultDatabasePath))
        {
            Log.Logger.Warning(
                "Using legacy config path {Legacy} because it contains {Database}. " +
                "Map your host appdata folder to container path {Custom} to persist data across updates.",
                defaultConfigFolder,
                DatabaseFileName,
                customConfigFolder);

            return defaultConfigFolder;
        }

        return customConfigFolder;
    }
}
