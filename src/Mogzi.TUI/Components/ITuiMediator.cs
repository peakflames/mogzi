namespace Mogzi.TUI.Components;

/// <summary>
/// Coordinates interactions between TUI components and manages complex workflows.
/// The mediator pattern reduces coupling between components by centralizing communication.
/// </summary>
public interface ITuiMediator
{
    /// <summary>
    /// Gets the name of this mediator for identification purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Handles user input coordination across components.
    /// </summary>
    /// <param name="input">The user input to process</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleUserInputAsync(string input, ITuiContext context);

    /// <summary>
    /// Handles key press events and coordinates them across components.
    /// </summary>
    /// <param name="e">The key press event arguments</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleKeyPressAsync(KeyPressEventArgs e, ITuiContext context);

    /// <summary>
    /// Handles character typed events and coordinates them across components.
    /// </summary>
    /// <param name="e">The character typed event arguments</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleCharacterTypedAsync(CharacterTypedEventArgs e, ITuiContext context);

    /// <summary>
    /// Handles state change notifications and coordinates component updates.
    /// </summary>
    /// <param name="newState">The new application state</param>
    /// <param name="previousState">The previous application state</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleStateChangeAsync(ChatState newState, ChatState previousState, ITuiContext context);

    /// <summary>
    /// Handles tool execution coordination and progress updates.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed</param>
    /// <param name="progress">The progress information</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task HandleToolExecutionAsync(string toolName, string progress, ITuiContext context);

    /// <summary>
    /// Notifies a specific component of an event or state change.
    /// </summary>
    /// <param name="componentName">The name of the component to notify</param>
    /// <param name="eventData">The event data to send</param>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task NotifyComponentAsync(string componentName, object eventData, ITuiContext context);

    /// <summary>
    /// Registers a component with the mediator for coordination.
    /// </summary>
    /// <param name="component">The component to register</param>
    void RegisterComponent(ITuiComponent component);

    /// <summary>
    /// Unregisters a component from the mediator.
    /// </summary>
    /// <param name="component">The component to unregister</param>
    void UnregisterComponent(ITuiComponent component);

    /// <summary>
    /// Notifies the mediator that the chat history has changed.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task NotifyHistoryChangedAsync();

    /// <summary>
    /// Starts the AI processing workflow.
    /// </summary>
    /// <param name="context">The TUI context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task StartAiProcessingWorkflow(ITuiContext context);
}
