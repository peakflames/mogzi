using MaxBot.TUI.App;
using MaxBot.TUI.State;
using MaxBot.PawPrints;
using MaxBot.Domain;
using MaxBot.Services;

namespace MaxBot.TUI.Infrastructure;

/// <summary>
/// Shared service configuration for dependency injection.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    /// Configures the dependency injection container.
    /// </summary>
    public static void ConfigureServices(IServiceCollection services, string? configPath = null, string? profileName = null, string? toolApprovals = null)
    {
        // Add logging - file logging only to keep UI clean
        services.AddLogging(builder =>
        {
            builder.AddProvider(new FileLoggerProvider(LogLevel.Warning));
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Add Spectre.Console
        services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);

        // Add core services
        services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
        
        // Create ChatClient - this will be configured per command based on settings
        var chatClientResult = ChatClient.Create(
            configPath ?? "maxbot.config.json",
            profileName, // Use specified profile or default
            toolApprovals, // Use specified tool approvals override
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
