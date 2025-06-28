

namespace UI;

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
        // Check if we should run the Live widget prototype
        if (args.Length > 0 && args[0] == "--prototype")
        {
            return await UI.Prototypes.PrototypeRunner.RunPrototypeAsync(args);
        }

        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services, args);
        
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Create and run the simplified FlexColumn TUI application
            using var app = new FlexColumnTuiApp(serviceProvider);

            // Run the application
            return await app.RunAsync(args);
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

        // Add UI components
        services.AddSingleton<LayoutManager>();
        services.AddSingleton<HeaderComponent>();
        services.AddSingleton<StaticHistoryComponent>();
        services.AddSingleton<DynamicContentComponent>();
        services.AddSingleton<InputComponent>();
        services.AddSingleton<FooterComponent>();
        services.AddSingleton<AppComponent>();
        
        // Add the new flex column app component
        services.AddSingleton<FlexColumnAppComponent>();
    }
}
