
using System.Reflection;
using FluentResults;

namespace CLI;

public record CommandLineOptions
{
    public string Mode { get; init; } = "oneshot";
    public string ConfigPath { get; init; } = "maxbot.config.json";
    public string? ProfileName { get; init; }
    public string? UserPrompt { get; init; }
    public bool ShowStatus { get; init; }
    public bool ShowHelp { get; init; }
    public bool ShowVersion { get; init; }
    public string? ToolApprovals { get; init; }
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
    public static async Task<Result<CommandLineOptions>> ParseAsync(string[] args)
    {
        string defaultConfigPath = "maxbot.config.json";
        string configPath = defaultConfigPath;
        string? profileName = null;
        string mode = "oneshot";
        string? userPrompt = null;
        bool showStatus = false;
        bool showHelp = false;
        bool showVersion = false;
        string? toolApprovals = null;
        var remainingArgs = new List<string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--chat")
            {
                mode = "chat";
            }
            else if (args[i] == "-h" || args[i] == "--help")
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
            else if (args[i] == "-s" || args[i] == "--status")
            {
                showStatus = true;
            }
            else if (args[i] == "-v" || args[i] == "--version")
            {
                showVersion = true;
            }
            else if ((args[i] == "-ta" || args[i] == "--tool-approvals") && i + 1 < args.Length)
            {
                toolApprovals = args[i + 1];
                if (toolApprovals != "readonly" && toolApprovals != "all")
                {
                    return Result.Fail("Invalid value for --tool-approvals. Must be 'readonly' or 'all'.");
                }
                i++; // Skip the next argument
            }
            else if (args[i].StartsWith('-'))
            {
                return Result.Fail($"Invalid argument: {args[i]}");
            }
            else
            {
                remainingArgs.Add(args[i]);
            }
        }

        if (mode == "oneshot" && Console.IsInputRedirected)
        {
            var pipedInput = await Console.In.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(pipedInput))
            {
                userPrompt = pipedInput;
            }
        }

        if (remainingArgs.Count > 0)
        {
            var promptFromArgs = string.Join(" ", remainingArgs);
            if (!string.IsNullOrWhiteSpace(userPrompt))
            {
                userPrompt += Environment.NewLine + promptFromArgs;
            }
            else
            {
                userPrompt = promptFromArgs;
            }
        }

        if (args.Length == 0 && string.IsNullOrWhiteSpace(userPrompt))
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
            ShowVersion = showVersion,
            ToolApprovals = toolApprovals
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
        Console.WriteLine("Usage: max [prompt] [options]");
        Console.WriteLine("       max chat [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -c, --config <path>          Path to the configuration file (default: maxbot.config.json)");
        Console.WriteLine("  -p, --profile <name>         Name of the profile to use (overrides default profile in config)");
        Console.WriteLine("  -s, --status                 Report the current status (i.e. active config, etc)");
        Console.WriteLine("  -ta, --tool-approvals <mode> Override the tool approval setting (readonly|all)");
        Console.WriteLine("  -h, --help                   Display this help message");
        Console.ResetColor();
    }
}
