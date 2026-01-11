using System.Runtime.InteropServices;

namespace AIntern.Data;

/// <summary>
/// Resolves the database file path based on the current operating system.
/// Uses platform-appropriate application data directories.
/// </summary>
public static class DatabasePathResolver
{
    private const string AppName = "AIntern";
    private const string DatabaseFileName = "aintern.db";

    /// <summary>
    /// Gets the full path to the SQLite database file.
    /// </summary>
    /// <returns>
    /// Platform-specific path:
    /// - Windows: %APPDATA%\AIntern\aintern.db
    /// - macOS: ~/Library/Application Support/AIntern/aintern.db
    /// - Linux: ~/.config/AIntern/aintern.db
    /// </returns>
    public static string GetDatabasePath()
    {
        var appDataPath = GetAppDataDirectory();
        return Path.Combine(appDataPath, DatabaseFileName);
    }

    /// <summary>
    /// Gets the application data directory, creating it if it doesn't exist.
    /// </summary>
    public static string GetAppDataDirectory()
    {
        string basePath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: %APPDATA%\AIntern
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: ~/Library/Application Support/AIntern
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            basePath = Path.Combine(home, "Library", "Application Support");
        }
        else
        {
            // Linux and others: ~/.config/AIntern
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(xdgConfig))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                basePath = Path.Combine(home, ".config");
            }
            else
            {
                basePath = xdgConfig;
            }
        }

        var appDataPath = Path.Combine(basePath, AppName);

        // Ensure the directory exists
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        return appDataPath;
    }

    /// <summary>
    /// Gets the SQLite connection string for the database.
    /// </summary>
    public static string GetConnectionString()
    {
        return $"Data Source={GetDatabasePath()}";
    }
}
