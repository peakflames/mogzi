namespace UI.Core;

/// <summary>
/// Represents a UI component that can be rendered in the terminal interface.
/// </summary>
public interface ITuiComponent : IDisposable
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    string ComponentId { get; }

    /// <summary>
    /// Renders the component to a Spectre.Console IRenderable.
    /// </summary>
    /// <param name="context">The rendering context containing layout constraints and state.</param>
    /// <returns>A task that resolves to the rendered content.</returns>
    Task<IRenderable> RenderAsync(RenderContext context);

    /// <summary>
    /// Determines if the component should be re-rendered based on the current context.
    /// </summary>
    /// <param name="context">The current rendering context.</param>
    /// <returns>True if the component needs to be re-rendered, false otherwise.</returns>
    bool ShouldUpdate(RenderContext context);

    /// <summary>
    /// Called when the component is mounted to the UI tree.
    /// </summary>
    Task OnMountAsync();

    /// <summary>
    /// Called when the component is unmounted from the UI tree.
    /// </summary>
    Task OnUnmountAsync();
}
