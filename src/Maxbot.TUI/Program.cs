

using MaxBot.PawPrints;

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
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Spectre.Console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Add core services
        services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
        
        var chatClientResult = ChatClient.Create(
            "maxbot.config.json",
            null, // Use default profile
            "readonly",
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
}
