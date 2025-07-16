using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mogzi.TUI.Infrastructure;

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
            _ = builder.AddProvider(new FileLoggerProvider(LogLevel.Trace));
            _ = builder.SetMinimumLevel(LogLevel.Trace);
        });

        // Add Spectre.Console
        services.TryAddSingleton(AnsiConsole.Console);

        // Add core services
        services.TryAddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();

        services.TryAddSingleton(serviceProvider =>
        {
            // Create ChatClient - this will be configured per command based on settings
            var logger = serviceProvider.GetRequiredService<ILogger<ChatClient>>();
            var chatClientResult = ChatClient.Create(
                serviceProvider.GetRequiredService<IWorkingDirectoryProvider>(),
                configPath, // Let Create handle finding the default path,
                profileName, // Use specified profile or default
                toolApprovals, // Use specified tool approvals override
                "chat",
                (details, color) =>
                {
                    // Log tool execution details to the log file instead of discarding them
                    logger.LogDebug("Tool execution: {Details}", details);
                },
                false
            );

            return chatClientResult.IsSuccess
                ? chatClientResult.Value
                : throw new InvalidOperationException($"Failed to create ChatClient: {string.Join(", ", chatClientResult.Errors.Select(e => e.Message))}");
        });


        _ = services.AddSingleton<IAppService, AppService>();
        _ = services.AddSingleton<HistoryManager>();
        _ = services.AddSingleton<SessionManager>();

        // Add autocomplete services
        _ = services.AddSingleton<SlashCommandProcessor>();
        _ = services.AddSingleton<IAutocompleteProvider, SlashCommandProvider>();
        _ = services.AddSingleton<IAutocompleteProvider, FilePathProvider>();
        _ = services.AddSingleton<AutocompleteManager>();

        // Add user selection services
        _ = services.AddSingleton<IUserSelectionProvider, ToolApprovalsProvider>();
        _ = services.AddSingleton<IUserSelectionProvider, SessionListProvider>();
        _ = services.AddSingleton<UserSelectionManager>();

        // Add TUI infrastructure components
        _ = services.AddSingleton<FlexColumnTuiApp>();
        _ = services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
        _ = services.AddSingleton<ToolResponseParser>();

        // Register keyboard handler as singleton to ensure same instance is used throughout
        _ = services.AddSingleton<AdvancedKeyboardHandler>();

        // Add state management components
        _ = services.AddSingleton<ITuiStateManager, TuiStateManager>();
        _ = services.AddSingleton<InputContext>();
        _ = services.AddSingleton<ITuiContext>(serviceProvider =>
        {
            var inputContext = serviceProvider.GetRequiredService<InputContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<TuiContext>>();
            var scrollbackTerminal = serviceProvider.GetRequiredService<IScrollbackTerminal>();
            var historyManager = serviceProvider.GetRequiredService<HistoryManager>();
            var sessionManager = serviceProvider.GetRequiredService<SessionManager>();
            var autocompleteManager = serviceProvider.GetRequiredService<AutocompleteManager>();
            var userSelectionManager = serviceProvider.GetRequiredService<UserSelectionManager>();
            var slashCommandProcessor = serviceProvider.GetRequiredService<SlashCommandProcessor>();
            var workingDirectoryProvider = serviceProvider.GetRequiredService<IWorkingDirectoryProvider>();
            var toolResponseParser = serviceProvider.GetRequiredService<ToolResponseParser>();
            var appService = serviceProvider.GetRequiredService<IAppService>();
            var aiProcessingCoordinator = serviceProvider.GetRequiredService<IAiProcessingCoordinator>();

            return new TuiContext(
                inputContext,
                serviceProvider,
                logger,
                scrollbackTerminal,
                historyManager,
                sessionManager,
                autocompleteManager,
                userSelectionManager,
                slashCommandProcessor,
                workingDirectoryProvider,
                toolResponseParser,
                appService,
                aiProcessingCoordinator);
        });

        // Register state implementations
        _ = services.AddTransient<InputTuiState>();
        _ = services.AddTransient<ThinkingTuiState>();
        _ = services.AddTransient<ToolExecutionTuiState>();

        // Register component architecture services
        _ = services.AddSingleton<IRenderingUtilities, RenderingUtilities>();
        _ = services.AddSingleton<IThemeInfo, DefaultThemeInfo>();
        _ = services.AddSingleton<ITuiLayout, FlexColumnLayout>();

        // Register TUI components
        _ = services.AddSingleton<InputPanel>();
        _ = services.AddSingleton<AutocompletePanel>();
        _ = services.AddSingleton<UserSelectionPanel>();
        _ = services.AddSingleton<ProgressPanel>();
        _ = services.AddSingleton<FooterPanel>();
        _ = services.AddSingleton<WelcomePanel>();

        // Register and configure component manager
        _ = services.AddSingleton<ITuiComponentManager>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TuiComponentManager>>();
            var componentManager = new TuiComponentManager(logger);
            var layout = serviceProvider.GetRequiredService<ITuiLayout>();

            // Register all components
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<InputPanel>());
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<AutocompletePanel>());
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<UserSelectionPanel>());
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<ProgressPanel>());
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<FooterPanel>());
            componentManager.RegisterComponent(serviceProvider.GetRequiredService<WelcomePanel>());

            // Set the layout
            componentManager.CurrentLayout = layout;

            return componentManager;
        });

        // Register and configure AI processing coordinator
        _ = services.AddSingleton<IAiProcessingCoordinator>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<AiProcessingCoordinator>>();
            var coordinator = new AiProcessingCoordinator(logger);

            // Register components with coordinator
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<InputPanel>());
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<AutocompletePanel>());
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<UserSelectionPanel>());
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<ProgressPanel>());
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<FooterPanel>());
            coordinator.RegisterComponent(serviceProvider.GetRequiredService<WelcomePanel>());

            return coordinator;
        });
    }
}
