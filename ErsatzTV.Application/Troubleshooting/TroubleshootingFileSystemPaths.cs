namespace ErsatzTV.Application.Troubleshooting;

public record TroubleshootingFileSystemPaths(
    string AppDataFolder,
    string LegacyAppDataFolder,
    string ConfigFolderEnv,
    string DatabasePath,
    string LogsFolder,
    string LogFilePath,
    string TranscodeFolder,
    bool DatabaseFileExists,
    bool LogFileExists);
