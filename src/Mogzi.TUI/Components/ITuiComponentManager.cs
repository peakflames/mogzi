namespace Mogzi.TUI.Components;

/// <summary>
/// Manages the lifecycle and coordination of TUI components.
/// Handles component registration, layout composition, and input distribution.
/// </summary>
public interface ITuiComponentManager
{
    /// <summary>
    /// Gets all registered components.
    /// </summary>
    IReadOnlyDictionary<string, ITuiComponent> Components { get; }

    /// <summary>
    /// Gets the current layout being used for component arrangement.
    /// </summary>
    ITuiLayout? CurrentLayout { get; set; }

    /// <summary>
    /// Registers a component with the manager.
    /// </summary>
    /// <param name="component">The component to register</param>
    void RegisterComponent(ITuiComponent component);

    /// <summary>
    /// Unregisters a component from the manager.
    /// </summary>
    /// <param name="componentName">The name of the component to unregister</param>
    /// <returns>True if the component was found and removed, false otherwise</returns>
    bool UnregisterComponent(string componentName);

    /// <summary>
    /// Gets a component by name.
    /// </summary>
    /// <param name="componentName">The name of the component to retrieve</param>
    /// <returns>The component if found, null otherwise</returns>
    ITuiComponent? GetComponent(string componentName);

    /// <summary>
    /// Gets a component by name and type.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve</typeparam>
    /// <param name="componentName">The name of the component to retrieve</param>
    /// <returns>The component if found and of the correct type, null otherwise</returns>
    T? GetComponent<T>(string componentName) where T : class, ITuiComponent;

    /// <summary>
    /// Renders the current layout with all visible components.
    /// </summary>
    /// <param name="context">The render context</param>
    /// <returns>The composed layout renderable</returns>
    IRenderable RenderLayout(IRenderContext context);

    /// <summary>
    /// Broadcasts an input event to all relevant components.
    /// </summary>
    /// <param name="inputEvent">The input event to broadcast</param>
    /// <param name="context">The render context</param>
    /// <returns>True if any component handled the input, false otherwise</returns>
    Task<bool> BroadcastInputAsync(object inputEvent, IRenderContext context);

    /// <summary>
    /// Initializes all registered components.
    /// </summary>
    /// <param name="context">The render context</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task InitializeComponentsAsync(IRenderContext context);

    /// <summary>
    /// Disposes all registered components.
    /// </summary>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DisposeComponentsAsync();

    /// <summary>
    /// Sets the visibility of a component.
    /// </summary>
    /// <param name="componentName">The name of the component</param>
    /// <param name="isVisible">Whether the component should be visible</param>
    void SetComponentVisibility(string componentName, bool isVisible);

    /// <summary>
    /// Updates the visibility of components based on the current application state.
    /// </summary>
    /// <param name="currentState">The current application state</param>
    /// <param name="context">The render context</param>
    void UpdateComponentVisibility(ChatState currentState, IRenderContext context);
}

/// <summary>
/// Defines how components are arranged in the TUI layout.
/// </summary>
public interface ITuiLayout
{
    /// <summary>
    /// Gets the name of this layout for identification purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the components required by this layout.
    /// </summary>
    /// <returns>A collection of component names required by this layout</returns>
    IEnumerable<string> GetRequiredComponents();

    /// <summary>
    /// Composes the layout using the provided components.
    /// </summary>
    /// <param name="components">The available components</param>
    /// <param name="context">The render context</param>
    /// <returns>The composed layout renderable</returns>
    IRenderable Compose(IReadOnlyDictionary<string, ITuiComponent> components, IRenderContext context);

    /// <summary>
    /// Validates that all required components are available.
    /// </summary>
    /// <param name="availableComponents">The available components</param>
    /// <returns>True if all required components are available, false otherwise</returns>
    bool ValidateComponents(IReadOnlyDictionary<string, ITuiComponent> availableComponents);
}
