
using System.Reflection;
using FluentResults;

namespace CLI;

public record CommandLineOptions
{
    public string? Mode { get; init; } = "chat";
    public string ConfigPath { get; init; } = "maxbot.config.json";
    public string? ProfileName { get; init; }
    public string? UserPrompt { get; init; }
    public bool ShowStatus { get; init; }
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
}


public static class CliArgParser
{
    public static string? Version
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        }
    }
    public static Result<CommandLineOptions> Parse(string[] args)
    {
        string defaultConfigPath = "maxbot.config.json";
        string configPath = defaultConfigPath;
        string? profileName = null;
        string? mode = null;
        string? userPrompt = null;
        bool showStatus = false;
        bool showHelp = false;
        bool showVersion = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (i == 0 && args[i] == "-h" || args[i] == "--help")
            {
                showHelp = true;
            }
            else if ((args[i] == "-c" || args[i] == "--config") && i + 1 < args.Length)
            {
                configPath = args[i + 1];
                i++; // Skip the next argument
            }
            else if ((args[i] == "-p" || args[i] == "--profile") && i + 1 < args.Length)
            {
                profileName = args[i + 1];
                i++; // Skip the next argument
            }
            else if ((args[i] == "-m" || args[i] == "--mode") && i + 1 < args.Length)
            {
                mode = args[i + 1];
                i++; // Skip the next argument
            }
            else if ((args[i] == "-u" || args[i] == "--userPrompt") && i + 1 < args.Length)
            {
                userPrompt = args[i + 1];
                i++; // Skip the next argument
            }
            else if (args[i] == "-s" || args[i] == "--status")
            {
                showStatus = true;
            }
            else if (args[i] == "-v" || args[i] == "--version")
            {
                showVersion = true;
            }
            else if (args[i].StartsWith('-'))
            {
                return Result.Fail($"Invalid argument: {args[i]}");
            }
            else
            {
                userPrompt = args[i];
            }
        }

        if (args.Length == 0)
        {
            showHelp = true;
        }

        configPath = ResolveConfigPath(configPath, defaultConfigPath);

        return new CommandLineOptions
        {
            ConfigPath = configPath,
            ProfileName = profileName,
            Mode = mode,
            UserPrompt = userPrompt,
            ShowStatus = showStatus,
            ShowHelp = showHelp,
            ShowVersion = showVersion
        };
    }

    private static string ResolveConfigPath(string? configPath, string defaultConfigPath)
    {
        if (string.IsNullOrEmpty(configPath))
        {
            return defaultConfigPath;
        }

        if (configPath == defaultConfigPath)
        {
            // Check if the config file exists in the current directory
            if (File.Exists(configPath))
            {
                return configPath;
            }

            // Check if the config file exists in the user's home directory
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (homeDirectory != null)
            {
                var configPathInHomeDirectory = Path.Combine(homeDirectory, configPath);
                if (File.Exists(configPathInHomeDirectory))
                {
                    return configPathInHomeDirectory;
                }
            }
        }

        return configPath;
    }

    // Display help information
    public static void DisplayHelp()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("MaxBot CLI - Chat with AI models");
        Console.WriteLine($"v{Version}");
        Console.WriteLine();
        Console.WriteLine("Usage: max prompt [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --config <path>          Path to the configuration file (default: maxbot.config.json)");
        Console.WriteLine("  -p, --profile <name>         Name of the profile to use (overrides default profile in config)");
        Console.WriteLine("  -m, --mode <mode>            chat or oneshot (overrides default mode in config)");
        Console.WriteLine("  -u, --userPrompt <prompt>    Oneshot user prompt");
        Console.WriteLine("  -s, --status                 Report the current status (i.e. active config, etc)");
        Console.WriteLine("  -h, --help                   Display this help message");
        Console.ResetColor();
    }
}

