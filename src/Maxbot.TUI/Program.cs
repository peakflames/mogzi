using Spectre.Console.Cli;
using MaxBot.TUI.Commands;
using System.Reflection;

namespace MaxBot.TUI;

/// <summary>
/// Entry point for the UI application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        // If no arguments provided, default to chat mode
        if (args.Length == 0)
        {
            args = new[] { "chat" };
        }
        
        // We don't need to setup DI here since each command handles its own
        
        // Create the command app without complex DI integration
        var app = new CommandApp();
        
        app.Configure(config =>
        {
            config.SetApplicationName("MaxBot");
            config.SetApplicationVersion(GetApplicationVersion());
            
            // Add commands
            config.AddCommand<ChatCommand>("chat")
                .WithDescription("Start interactive chat mode (default)")
                .WithExample(new[] { "chat" })
                .WithExample(new[] { "chat", "--verbosity", "normal" })
                .WithExample(new[] { "chat", "--profile", "Sonnet" });
                
            config.AddCommand<NonInteractiveCommand>("run")
                .WithDescription("Run a single prompt non-interactively")
                .WithExample(new[] { "run", "--prompt", "What is the capital of Michigan?" })
                .WithExample(new[] { "run", "--prompt", "./prompts/analyze-code.md" })
                .WithExample(new[] { "run", "--prompt", "\"Explain the code in this directory\"", "--no-history" });
        });

        try
        {
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Gets the application version from the assembly.
    /// </summary>
    /// <returns>The application version string.</returns>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "UNKNOWN";
        }
        catch
        {
            return "UNKNOWN";
        }
    }
}
