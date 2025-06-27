using UI.Components;

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
        // Setup dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Create and run the TUI application
            using var app = new TuiApp(serviceProvider);
            
            // Register the main application component
            var appComponent = serviceProvider.GetRequiredService<AppComponent>();
            app.RegisterComponent(appComponent, RenderZone.Static);

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
    private static void ConfigureServices(IServiceCollection services)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Spectre.Console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Add UI components
        services.AddSingleton<LayoutManager>();
        services.AddSingleton<HeaderComponent>();
        services.AddSingleton<StaticHistoryComponent>();
        services.AddSingleton<DynamicContentComponent>();
        services.AddSingleton<InputComponent>();
        services.AddSingleton<FooterComponent>();
        services.AddSingleton<AppComponent>();
    }
}
