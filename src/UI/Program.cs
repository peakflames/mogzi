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
            
            // Register a simple demo component
            var demoComponent = new DemoComponent();
            app.RegisterComponent(demoComponent, RenderZone.Static);

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
    }
}

/// <summary>
/// Simple demo component to test the UI framework.
/// </summary>
internal sealed class DemoComponent : TuiComponentBase
{
    private readonly TuiState<int> _counter;
    private readonly TuiState<DateTime> _lastUpdate;

    public DemoComponent()
    {
        _counter = UseState(0, "counter");
        _lastUpdate = UseState(DateTime.UtcNow, "lastUpdate");

        // Update counter every second
        UseEffect(async () =>
        {
            while (!IsDisposed)
            {
                await Task.Delay(1000);
                if (!IsDisposed)
                {
                    _counter.Value++;
                    _lastUpdate.Value = DateTime.UtcNow;
                }
            }
        }, Array.Empty<object>());
    }

    public override async Task<IRenderable> RenderAsync(RenderContext context)
    {
        await Task.CompletedTask;

        var panel = new Panel(new Rows(
            new Text($"MaxBot UI Framework Demo"),
            new Text($"Counter: {_counter.Value}"),
            new Text($"Last Update: {_lastUpdate.Value:HH:mm:ss}"),
            new Text($"Terminal Size: {context.TerminalSize.Width}x{context.TerminalSize.Height}"),
            new Text($"Component ID: {ComponentId[..8]}..."),
            new Text(""),
            new Text("Press Ctrl+C to exit")
        ))
        .Header("Demo Component")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Green);

        return panel;
    }

    public override bool ShouldUpdate(RenderContext context)
    {
        // Update every time since we have a counter
        return true;
    }
}
