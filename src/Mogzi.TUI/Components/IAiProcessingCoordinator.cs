namespace Mogzi.TUI.Components;

/// <summary>
/// Coordinates AI processing workflows and manages component registration.
/// This interface focuses on the core functionality that is actually used.
/// </summary>
public interface IAiProcessingCoordinator
{
    /// <summary>
    /// Gets the name of this coordinator for identification purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers a component with the coordinator.
    /// </summary>
    /// <param name="component">The component to register</param>
    void RegisterComponent(ITuiComponent component);

    /// <summary>
    /// Notifies the coordinator that the chat history has changed.
    /// This is a placeholder for future functionality.
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
