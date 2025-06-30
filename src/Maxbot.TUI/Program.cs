

using MaxBot.PawPrints;
using MaxBot.TUI.Infrastructure;

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
        // Check if terminal is ready for TUI operations
        /*if (!ConsoleExtensions.IsTerminalReady())
        {
            Console.WriteLine("Error: Terminal is not suitable for TUI operations.");
            Console.WriteLine("Please run this application in a proper terminal environment.");
            return 1;
        }*/

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, args);
        
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Create and run the FlexColumn TUI application
            var app = serviceProvider.GetRequiredService<FlexColumnTuiApp>();

            // Run the application
            await app.RunAsync(args);
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Application cancelled by user.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            return 1;
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Configures the dependency injection container.
    /// </summary>
    private static void ConfigureServices(IServiceCollection services, string[] args)
    {
        // Parse verbosity level from command line arguments
        var logLevel = ParseVerbosityLevel(args);
        
        // Add logging - always log to file, verbosity controls level
        services.AddLogging(builder =>
        {
            // Always add file logging to ~/.max/logs
            builder.AddProvider(new FileLoggerProvider(logLevel));
            builder.SetMinimumLevel(logLevel);
            
            // No console logging - keep UI clean
        });

        // Add Spectre.Console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Add core services
        services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
        
        var chatClientResult = ChatClient.Create(
            "maxbot.config.json",
            null, // Use default profile
            null,
            "chat",
            (details, color) => {},
            false
        );

        if (chatClientResult.IsSuccess)
        {
            services.AddSingleton(chatClientResult.Value);
        }
        else
        {
            throw new InvalidOperationException($"Failed to create ChatClient: {string.Join(", ", chatClientResult.Errors.Select(e => e.Message))}");
        }

        services.AddSingleton<IAppService, AppService>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<StateManager>();

        // Add TUI infrastructure components
        services.AddSingleton<FlexColumnTuiApp>();
        services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
    }

    /// <summary>
    /// Parses the verbosity level from command line arguments.
    /// Supports MSBuild-style verbosity levels: quiet, minimal, normal, detailed, diagnostic.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The corresponding LogLevel, or LogLevel.None for quiet/no logging.</returns>
    private static LogLevel ParseVerbosityLevel(string[] args)
    {
        // Default to no logging (quiet operation)
        var defaultLevel = LogLevel.None;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            // Check for --verbosity flag
            if (arg.Equals("--verbosity", StringComparison.OrdinalIgnoreCase) || 
                arg.Equals("-v", StringComparison.OrdinalIgnoreCase))
            {
                // Get the next argument as the verbosity level
                if (i + 1 < args.Length)
                {
                    var level = args[i + 1].ToLowerInvariant();
                    return level switch
                    {
                        "q" or "quiet" => LogLevel.None,
                        "m" or "minimal" => LogLevel.Error,
                        "n" or "normal" => LogLevel.Information,
                        "d" or "detailed" => LogLevel.Debug,
                        "diag" or "diagnostic" => LogLevel.Trace,
                        _ => defaultLevel
                    };
                }
            }
            // Check for combined format like --verbosity=normal
            else if (arg.StartsWith("--verbosity=", StringComparison.OrdinalIgnoreCase))
            {
                var level = arg.Substring("--verbosity=".Length).ToLowerInvariant();
                return level switch
                {
                    "q" or "quiet" => LogLevel.None,
                    "m" or "minimal" => LogLevel.Error,
                    "n" or "normal" => LogLevel.Information,
                    "d" or "detailed" => LogLevel.Debug,
                    "diag" or "diagnostic" => LogLevel.Trace,
                    _ => defaultLevel
                };
            }
            // Check for short combined format like -v=normal
            else if (arg.StartsWith("-v=", StringComparison.OrdinalIgnoreCase))
            {
                var level = arg.Substring("-v=".Length).ToLowerInvariant();
                return level switch
                {
                    "q" or "quiet" => LogLevel.None,
                    "m" or "minimal" => LogLevel.Error,
                    "n" or "normal" => LogLevel.Information,
                    "d" or "detailed" => LogLevel.Debug,
                    "diag" or "diagnostic" => LogLevel.Trace,
                    _ => defaultLevel
                };
            }
        }

        return defaultLevel;
    }
}
