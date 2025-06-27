namespace UI.Core;

/// <summary>
/// Provides context information for component rendering including layout constraints and state.
/// </summary>
public sealed class RenderContext
{
    /// <summary>
    /// The layout constraints for this rendering context.
    /// </summary>
    public LayoutConstraints Constraints { get; }

    /// <summary>
    /// The current terminal size.
    /// </summary>
    public TerminalSize TerminalSize { get; }

    /// <summary>
    /// Indicates if this is a forced re-render (ignoring ShouldUpdate).
    /// </summary>
    public bool ForceRender { get; }

    /// <summary>
    /// The current rendering timestamp.
    /// </summary>
    public DateTime RenderTime { get; }

    /// <summary>
    /// Additional context data that can be passed between components.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; }

    public RenderContext(
        LayoutConstraints constraints,
        TerminalSize terminalSize,
        bool forceRender = false,
        IReadOnlyDictionary<string, object>? data = null)
    {
        Constraints = constraints;
        TerminalSize = terminalSize;
        ForceRender = forceRender;
        RenderTime = DateTime.UtcNow;
        Data = data ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a new RenderContext with modified constraints for a child component.
    /// </summary>
    public RenderContext WithConstraints(LayoutConstraints newConstraints)
    {
        return new RenderContext(newConstraints, TerminalSize, ForceRender, Data);
    }

    /// <summary>
    /// Creates a new RenderContext with additional data.
    /// </summary>
    public RenderContext WithData(string key, object value)
    {
        var newData = new Dictionary<string, object>(Data) { [key] = value };
        return new RenderContext(Constraints, TerminalSize, ForceRender, newData);
    }

    /// <summary>
    /// Creates a new RenderContext for a specific layout zone.
    /// </summary>
    public RenderContext ForZone(string zoneName, int height, int width = -1)
    {
        var newConstraints = new LayoutConstraints(
            height,
            width == -1 ? Constraints.Width : width,
            Constraints.X,
            Constraints.Y
        );
        return WithConstraints(newConstraints).WithData("zone", zoneName);
    }
}

/// <summary>
/// Defines layout constraints for component rendering.
/// </summary>
public sealed record LayoutConstraints(
    int Height,
    int Width,
    int X = 0,
    int Y = 0)
{
    /// <summary>
    /// Creates constraints that fill the available space.
    /// </summary>
    public static LayoutConstraints Fill(int width, int height) => new(height, width);

    /// <summary>
    /// Creates constraints with a fixed height and full width.
    /// </summary>
    public static LayoutConstraints FixedHeight(int height, int width) => new(height, width);

    /// <summary>
    /// Creates constraints with flexible height (minimum 1).
    /// </summary>
    public static LayoutConstraints Flexible(int width, int minHeight = 1) => new(minHeight, width);
}

/// <summary>
/// Represents the current terminal size.
/// </summary>
public sealed record TerminalSize(int Width, int Height)
{
    /// <summary>
    /// Gets the current terminal size from the console.
    /// </summary>
    public static TerminalSize Current => new(Console.WindowWidth, Console.WindowHeight);

    /// <summary>
    /// Checks if the terminal is considered small (less than 80 columns).
    /// </summary>
    public bool IsSmall => Width < 80;

    /// <summary>
    /// Checks if the terminal is considered large (more than 120 columns).
    /// </summary>
    public bool IsLarge => Width > 120;
}
