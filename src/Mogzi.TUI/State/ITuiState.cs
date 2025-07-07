namespace Mogzi.TUI.State;

/// <summary>
/// Represents a state in the TUI application state machine.
/// Each state handles specific UI rendering and input processing logic.
/// </summary>
public interface ITuiState
{
    /// <summary>
    /// Gets the name of this state for identification and logging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Renders the dynamic content for this state.
    /// This method is called continuously during the UI refresh cycle.
    /// </summary>
    /// <param name="context">The shared context containing state and services</param>
    /// <returns>The renderable content for this state</returns>
    IRenderable RenderDynamicContent(ITuiContext context);

    /// <summary>
    /// Handles key press events for this state.
    /// </summary>
    /// <param name="context">The shared context containing state and services</param>
    /// <param name="e">The key press event arguments</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleKeyPressAsync(ITuiContext context, KeyPressEventArgs e);

    /// <summary>
    /// Handles character typed events for this state.
    /// </summary>
    /// <param name="context">The shared context containing state and services</param>
    /// <param name="e">The character typed event arguments</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleCharacterTypedAsync(ITuiContext context, CharacterTypedEventArgs e);

    /// <summary>
    /// Called when entering this state. Use for initialization and setup.
    /// </summary>
    /// <param name="context">The shared context containing state and services</param>
    /// <param name="previousState">The state being transitioned from, if any</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task OnEnterAsync(ITuiContext context, ITuiState? previousState);

    /// <summary>
    /// Called when exiting this state. Use for cleanup and teardown.
    /// </summary>
    /// <param name="context">The shared context containing state and services</param>
    /// <param name="nextState">The state being transitioned to, if any</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task OnExitAsync(ITuiContext context, ITuiState? nextState);
}
