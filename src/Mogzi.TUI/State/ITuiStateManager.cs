namespace Mogzi.TUI.State;

/// <summary>
/// Manages the state machine for the TUI application.
/// Handles state transitions, input delegation, and state lifecycle management.
/// </summary>
public interface ITuiStateManager
{
    /// <summary>
    /// Gets the current active state.
    /// </summary>
    ITuiState? CurrentState { get; }

    /// <summary>
    /// Gets the current state type for compatibility with existing code.
    /// </summary>
    ChatState CurrentStateType { get; }

    /// <summary>
    /// Initializes the state manager with the provided context.
    /// </summary>
    /// <param name="context">The TUI context to use</param>
    Task InitializeAsync(ITuiContext context);

    /// <summary>
    /// Transitions to the specified state.
    /// </summary>
    /// <param name="newStateType">The state type to transition to</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task TransitionToStateAsync(ChatState newStateType);

    /// <summary>
    /// Renders the dynamic content for the current state.
    /// </summary>
    /// <returns>The renderable content</returns>
    IRenderable RenderDynamicContent();

    /// <summary>
    /// Handles key press events by delegating to the current state.
    /// </summary>
    /// <param name="e">The key press event arguments</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleKeyPressAsync(KeyPressEventArgs e);

    /// <summary>
    /// Handles character typed events by delegating to the current state.
    /// </summary>
    /// <param name="e">The character typed event arguments</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleCharacterTypedAsync(CharacterTypedEventArgs e);

    /// <summary>
    /// Registers a state implementation for the specified state type.
    /// </summary>
    /// <param name="stateType">The state type</param>
    /// <param name="stateFactory">Factory function to create the state instance</param>
    void RegisterState(ChatState stateType, Func<ITuiState> stateFactory);

    /// <summary>
    /// Shuts down the state manager and cleans up resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ShutdownAsync();
}
