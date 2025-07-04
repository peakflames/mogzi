namespace Mogzi.TUI.Components;

/// <summary>
/// Provides rendering context and services for TUI components.
/// Acts as a bridge between components and the application infrastructure.
/// </summary>
public class RenderContext(
    ITuiContext tuiContext,
    ChatState currentState,
    ILogger logger,
    IServiceProvider serviceProvider,
    IRenderingUtilities renderingUtilities,
    IThemeInfo? themeInfo = null) : IRenderContext
{
    public ITuiContext TuiContext { get; } = tuiContext ?? throw new ArgumentNullException(nameof(tuiContext));
    public ChatState CurrentState { get; } = currentState;
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    public IThemeInfo? ThemeInfo { get; } = themeInfo;
    public IRenderingUtilities RenderingUtilities { get; } = renderingUtilities ?? throw new ArgumentNullException(nameof(renderingUtilities));
}

/// <summary>
/// Default theme information for the TUI application.
/// </summary>
public class DefaultThemeInfo : IThemeInfo
{
    public Color PrimaryColor => Color.Blue;
    public Color SecondaryColor => Color.Grey23;
    public Color AccentColor => Color.Green;
    public BoxBorder BorderStyle => BoxBorder.Rounded;
}
