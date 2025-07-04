namespace Mogzi.TUI.Components;

/// <summary>
/// Represents a UI component in the TUI application.
/// Components are modular, reusable pieces of UI that can be composed into layouts.
/// </summary>
public interface ITuiComponent
{
    /// <summary>
    /// Gets the name of this component for identification and logging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets whether this component is currently visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Renders the component content.
    /// </summary>
    /// <param name="context">The render context containing state and services</param>
    /// <returns>The renderable content for this component</returns>
    IRenderable Render(IRenderContext context);

    /// <summary>
    /// Handles input events for this component.
    /// </summary>
    /// <param name="context">The render context containing state and services</param>
    /// <param name="inputEvent">The input event to handle</param>
    /// <returns>True if the input was handled, false otherwise</returns>
    Task<bool> HandleInputAsync(IRenderContext context, object inputEvent);

    /// <summary>
    /// Called when the component is initialized.
    /// </summary>
    /// <param name="context">The render context containing state and services</param>
    Task InitializeAsync(IRenderContext context);

    /// <summary>
    /// Called when the component is being disposed.
    /// </summary>
    Task DisposeAsync();
}
