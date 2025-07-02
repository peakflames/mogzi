namespace MaxBot.Utils;

/// <summary>
/// Helper class to locate the maxbot.config.json file.
/// </summary>
public static class ConfigurationLocator
{
    private const string ConfigFileName = "maxbot.config.json";

    /// <summary>
    /// Finds the path to the configuration file by checking the current directory
    /// and falling back to the user's home directory.
    /// </summary>
    /// <returns>The full path to the config file, or null if not found.</returns>
    public static string? FindConfigPath()
    {
        // 1. Check current working directory
        var localPath = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
        if (File.Exists(localPath))
        {
            return localPath;
        }

        // 2. Fallback to user's home directory
        var homePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ConfigFileName);
        if (File.Exists(homePath))
        {
            return homePath;
        }

        // Not found
        return null;
    }
}
