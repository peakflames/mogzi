namespace Mogzi.TUI.Components;

/// <summary>
/// Represents a container component that can hold child components.
/// Panels provide hierarchical composition and layout management.
/// </summary>
public interface ITuiPanel : ITuiComponent
{
    /// <summary>
    /// Gets the collection of child components.
    /// </summary>
    IReadOnlyList<ITuiComponent> Children { get; }

    /// <summary>
    /// Adds a child component to this panel.
    /// </summary>
    /// <param name="child">The child component to add</param>
    void AddChild(ITuiComponent child);

    /// <summary>
    /// Removes a child component from this panel.
    /// </summary>
    /// <param name="child">The child component to remove</param>
    /// <returns>True if the child was removed, false if it wasn't found</returns>
    bool RemoveChild(ITuiComponent child);

    /// <summary>
    /// Removes all child components from this panel.
    /// </summary>
    void ClearChildren();

    /// <summary>
    /// Gets the layout strategy for arranging child components.
    /// </summary>
    IPanelLayout Layout { get; set; }
}

/// <summary>
/// Defines how child components are arranged within a panel.
/// </summary>
public interface IPanelLayout
{
    /// <summary>
    /// Gets the name of this layout strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Arranges the child components and returns the composed renderable.
    /// </summary>
    /// <param name="children">The child components to arrange</param>
    /// <param name="context">The render context</param>
    /// <returns>The composed renderable containing all children</returns>
    IRenderable ArrangeChildren(IEnumerable<ITuiComponent> children, IRenderContext context);
}
