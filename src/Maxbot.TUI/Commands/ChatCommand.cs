using Spectre.Console.Cli;
using MaxBot.TUI.App;
using MaxBot.TUI.Infrastructure;
using System.ComponentModel;

namespace MaxBot.TUI.Commands;

/// <summary>
/// Settings for the chat command (interactive TUI mode).
/// </summary>
public sealed class ChatSettings : CommandSettings
{
    [CommandOption("-v|--verbosity <LEVEL>")]
    [Description("Set the verbosity level (quiet, minimal, normal, detailed, diagnostic)")]
    [DefaultValue("quiet")]
    public string Verbosity { get; init; } = "quiet";

    [CommandOption("--config <PATH>")]
    [Description("Path to the configuration file")]
    [DefaultValue("maxbot.config.json")]
    public string ConfigPath { get; init; } = "maxbot.config.json";

    [CommandOption("--profile <NAME>")]
    [Description("Configuration profile to use")]
    public string? Profile { get; init; }
}

/// <summary>
/// Interactive chat command that launches the TUI.
/// </summary>
public sealed class ChatCommand : AsyncCommand<ChatSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ChatSettings settings)
    {
        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            // Create and run the FlexColumn TUI application
            var app = serviceProvider.GetRequiredService<FlexColumnTuiApp>();
            
            // Convert settings to args format for compatibility with existing TUI
            var args = BuildArgsFromSettings(settings);
            
            return await app.RunAsync(args);
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine("Application cancelled by user.");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static string[] BuildArgsFromSettings(ChatSettings settings)
    {
        var args = new List<string>();
        
        if (!string.IsNullOrEmpty(settings.Verbosity) && settings.Verbosity != "quiet")
        {
            args.Add("--verbosity");
            args.Add(settings.Verbosity);
        }
        
        if (!string.IsNullOrEmpty(settings.ConfigPath) && settings.ConfigPath != "maxbot.config.json")
        {
            args.Add("--config");
            args.Add(settings.ConfigPath);
        }
        
        if (!string.IsNullOrEmpty(settings.Profile))
        {
            args.Add("--profile");
            args.Add(settings.Profile);
        }
        
        return args.ToArray();
    }
}
