namespace Mogzi.TUI.State;

/// <summary>
/// Provides shared context and services for TUI states.
/// This interface acts as a bridge between states and the application infrastructure.
/// </summary>
public interface ITuiContext
{
    /// <summary>
    /// Gets the input context containing current input state and autocomplete information.
    /// </summary>
    InputContext InputContext { get; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the logger for this context.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the scrollback terminal for writing static content.
    /// </summary>
    IScrollbackTerminal ScrollbackTerminal { get; }

    /// <summary>
    /// Gets the history manager for chat session management.
    /// </summary>
    HistoryManager HistoryManager { get; }

    /// <summary>
    /// Gets the session manager for persistent session management.
    /// </summary>
    SessionManager SessionManager { get; }

    /// <summary>
    /// Gets the autocomplete manager for handling autocomplete functionality.
    /// </summary>
    AutocompleteManager AutocompleteManager { get; }

    /// <summary>
    /// Gets the user selection manager for interactive commands.
    /// </summary>
    UserSelectionManager UserSelectionManager { get; }

    /// <summary>
    /// Gets the slash command processor for handling slash commands.
    /// </summary>
    SlashCommandProcessor SlashCommandProcessor { get; }

    /// <summary>
    /// Gets the working directory provider for file operations.
    /// </summary>
    IWorkingDirectoryProvider WorkingDirectoryProvider { get; }

    /// <summary>
    /// Gets the tool response parser for processing tool outputs.
    /// </summary>
    ToolResponseParser ToolResponseParser { get; }

    /// <summary>
    /// Gets the application service for AI interactions.
    /// </summary>
    IAppService AppService { get; }

    /// <summary>
    /// Gets the TUI mediator for coordinating component interactions.
    /// </summary>
    ITuiMediator Mediator { get; }

    /// <summary>
    /// Gets the current tool progress text.
    /// </summary>
    string ToolProgress { get; set; }

    /// <summary>
    /// Gets the current tool name being executed.
    /// </summary>
    string CurrentToolName { get; set; }

    /// <summary>
    /// Gets the command history for navigation.
    /// </summary>
    List<string> CommandHistory { get; }

    /// <summary>
    /// Gets or sets the command history index for navigation.
    /// </summary>
    int CommandHistoryIndex { get; set; }

    /// <summary>
    /// Gets the AI operation start time for duration tracking.
    /// </summary>
    DateTime AiOperationStartTime { get; set; }

    /// <summary>
    /// Gets the AI operation cancellation token source.
    /// </summary>
    CancellationTokenSource? AiOperationCts { get; set; }

    /// <summary>
    /// Gets the function call to tool name mapping for tool execution tracking.
    /// </summary>
    Dictionary<string, string> FunctionCallToToolName { get; }

    /// <summary>
    /// Gets the function call to pre-edit content mapping for diff generation.
    /// </summary>
    Dictionary<string, string> FunctionCallToPreEditContent { get; }

    /// <summary>
    /// Gets or sets whether piped input should be automatically submitted.
    /// </summary>
    bool AutoSubmitPipedInput { get; set; }

    /// <summary>
    /// Requests a state transition to the specified state.
    /// </summary>
    /// <param name="newState">The state to transition to</param>
    Task RequestStateTransitionAsync(ChatState newState);

    /// <summary>
    /// Notifies that an event occurred that may require UI updates.
    /// </summary>
    void NotifyStateChanged();
}
