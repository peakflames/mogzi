namespace Mogzi.TUI.Components;

/// <summary>
/// Provides rendering context and services for TUI components.
/// This interface acts as a bridge between components and the application infrastructure.
/// </summary>
public interface IRenderContext
{
    /// <summary>
    /// Gets the TUI context containing shared state and services.
    /// </summary>
    ITuiContext TuiContext { get; }

    /// <summary>
    /// Gets the current application state.
    /// </summary>
    ChatState CurrentState { get; }

    /// <summary>
    /// Gets the logger for this render context.
    /// </summary>
    ILogger Logger { get; }

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets theme information for styling components.
    /// </summary>
    IThemeInfo? ThemeInfo { get; }

    /// <summary>
    /// Gets rendering utilities and helpers.
    /// </summary>
    IRenderingUtilities RenderingUtilities { get; }
}

/// <summary>
/// Provides theme information for component styling.
/// </summary>
public interface IThemeInfo
{
    /// <summary>
    /// Gets the primary color for the theme.
    /// </summary>
    Color PrimaryColor { get; }

    /// <summary>
    /// Gets the secondary color for the theme.
    /// </summary>
    Color SecondaryColor { get; }

    /// <summary>
    /// Gets the accent color for the theme.
    /// </summary>
    Color AccentColor { get; }

    /// <summary>
    /// Gets the border style for panels.
    /// </summary>
    BoxBorder BorderStyle { get; }

    /// <summary>
    /// Gets the color for user messages.
    /// </summary>
    string UserMessageColor { get; }

    /// <summary>
    /// Gets the color for assistant messages.
    /// </summary>
    string AssistantMessageColor { get; }

    /// <summary>
    /// Gets the color for system messages.
    /// </summary>
    string SystemMessageColor { get; }

    /// <summary>
    /// Gets the prefix for user messages.
    /// </summary>
    string UserMessagePrefix { get; }

    /// <summary>
    /// Gets the prefix for assistant messages.
    /// </summary>
    string AssistantMessagePrefix { get; }
}

/// <summary>
/// Provides rendering utilities and helpers for components.
/// </summary>
public interface IRenderingUtilities
{
    /// <summary>
    /// Formats a file path for display.
    /// </summary>
    /// <param name="fullPath">The full file path</param>
    /// <returns>The formatted display path</returns>
    string FormatDisplayPath(string fullPath);

    /// <summary>
    /// Formats model information for display.
    /// </summary>
    /// <param name="appService">The application service</param>
    /// <returns>The formatted model display string</returns>
    string FormatModelInfo(IAppService appService);

    /// <summary>
    /// Formats token usage information for display.
    /// </summary>
    /// <param name="appService">The application service</param>
    /// <param name="chatHistory">The chat history</param>
    /// <returns>The formatted token usage string</returns>
    string FormatTokenUsage(IAppService appService, IEnumerable<ChatMessage> chatHistory);

    /// <summary>
    /// Formats a token number with appropriate abbreviations (k, m).
    /// </summary>
    /// <param name="tokens">The token count</param>
    /// <returns>The formatted token number string</returns>
    string FormatTokenNumber(long tokens);

    /// <summary>
    /// Formats session token usage information for display.
    /// </summary>
    /// <param name="session">The session containing usage metrics</param>
    /// <returns>The formatted session token usage string</returns>
    string FormatSessionTokenUsage(Session? session);

    /// <summary>
    /// Formats cache usage information for display.
    /// </summary>
    /// <param name="session">The session containing cache metrics</param>
    /// <returns>The formatted cache usage string</returns>
    string FormatCacheUsage(Session? session);

    /// <summary>
    /// Formats context window usage information with color coding.
    /// </summary>
    /// <param name="appService">The application service</param>
    /// <param name="chatHistory">The chat history</param>
    /// <returns>The formatted context window usage string with color markup</returns>
    string FormatContextWindowUsage(IAppService appService, IEnumerable<ChatMessage> chatHistory);

    /// <summary>
    /// Renders a chat message with appropriate styling.
    /// </summary>
    /// <param name="message">The chat message to render</param>
    /// <param name="themeInfo">Optional theme information for styling</param>
    /// <returns>The rendered message</returns>
    IRenderable RenderMessage(ChatMessage message, IThemeInfo? themeInfo = null);
}
