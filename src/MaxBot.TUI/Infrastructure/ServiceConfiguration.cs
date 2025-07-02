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
        _ = services.AddLogging(builder =>
        {
            _ = builder.AddProvider(new FileLoggerProvider(LogLevel.Information));
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Spectre.Console
        _ = services.AddSingleton(AnsiConsole.Console);

        // Add core services
        _ = services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();

        // Create ChatClient - this will be configured per command based on settings
        var chatClientResult = ChatClient.Create(
            configPath ?? "maxbot.config.json",
            profileName, // Use specified profile or default
            toolApprovals, // Use specified tool approvals override
            "chat",
            (details, color) => { },
            false
        );

        _ = chatClientResult.IsSuccess
            ? services.AddSingleton(chatClientResult.Value)
            : throw new InvalidOperationException($"Failed to create ChatClient: {string.Join(", ", chatClientResult.Errors.Select(e => e.Message))}");

        _ = services.AddSingleton<IAppService, AppService>();
        _ = services.AddSingleton<HistoryManager>();
        _ = services.AddSingleton<StateManager>();

        // Add autocomplete services
        _ = services.AddSingleton<SlashCommandProcessor>();
        _ = services.AddSingleton<IAutocompleteProvider, SlashCommandProvider>();
        _ = services.AddSingleton<IAutocompleteProvider, FilePathProvider>();
        _ = services.AddSingleton<AutocompleteManager>();

        // Add user selection services
        _ = services.AddSingleton<IUserSelectionProvider, ToolApprovalsProvider>();
        _ = services.AddSingleton<UserSelectionManager>();

        // Add TUI infrastructure components
        _ = services.AddSingleton<FlexColumnTuiApp>();
        _ = services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
        _ = services.AddSingleton<ToolResponseParser>();
    }
}
