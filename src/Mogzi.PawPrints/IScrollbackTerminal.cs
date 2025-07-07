namespace Mogzi.PawPrints;

/// <summary>
/// A terminal controller that provides a scrollback buffer for static content
/// and a live-updating region for dynamic content, mimicking some of the
/// behavior of the 'ink' library for React.
/// </summary>
public interface IScrollbackTerminal
{
    /// <summary>
    /// Initializes the terminal, clearing the screen and preparing for rendering.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Writes a renderable object to the static scrollback history. This content
    /// will not be redrawn on subsequent updates.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="isUpdatable">If true, the next call to WriteStatic will overwrite this content.</param>
    void WriteStatic(IRenderable content, bool isUpdatable = false);

    /// <summary>
    /// Starts the rendering loop for the dynamic content area.
    /// </summary>
    /// <param name="dynamicContentProvider">A function that provides the dynamic content to render.</param>
    /// <param name="cancellationToken">A token to cancel the rendering loop.</param>
    /// <returns>A task that completes when the rendering loop exits.</returns>
    Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken);

    /// <summary>
    /// Cleans up resources and restores the terminal state.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Triggers an immediate refresh of the dynamic content area.
    /// </summary>
    void Refresh();
}
