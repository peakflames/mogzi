namespace Mogzi.TUI.Services;

/// <summary>
/// Provides user selection functionality for listing and selecting sessions.
/// Implements the IUserSelectionProvider interface to integrate with the TUI's user selection system.
/// </summary>
public class SessionListProvider(SessionManager sessionManager, ChatClient chatClient, HistoryManager historyManager, IScrollbackTerminal scrollbackTerminal, IServiceProvider serviceProvider, ILogger<SessionListProvider> logger) : IUserSelectionProvider
{
    private readonly SessionManager _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    private readonly HistoryManager _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
    private readonly IScrollbackTerminal _scrollbackTerminal = scrollbackTerminal ?? throw new ArgumentNullException(nameof(scrollbackTerminal));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger<SessionListProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Cache the session list to ensure consistency between GetSelectionsAsync and OnSelectionAsync
    private List<Session>? _cachedSessions;

    public string Command => "/session list";
    public string Description => "List and select from available sessions.";

    public async Task<List<CompletionItem>> GetSelectionsAsync()
    {
        var sessions = await _sessionManager.ListSessionsAsync();

        // Limit to the configured number of sessions (default 10) for better UX
        var sessionLimit = _chatClient.Config.SessionListLimit;
        var recentSessions = sessions.Take(sessionLimit).ToList();

        // Cache the sessions to ensure consistency during selection
        _cachedSessions = recentSessions;

        var selections = new List<CompletionItem>();
        foreach (var session in recentSessions)
        {
            var displayName = session.Name;
            var description = FormatSessionDescription(session);

            selections.Add(new CompletionItem(displayName, description));
        }

        return selections;
    }

    /// <summary>
    /// Formats a well-spaced, colorful description for a session entry with fixed-width columns.
    /// </summary>
    private static string FormatSessionDescription(Session session)
    {
        // Format the creation date in a more readable way
        var createdDate = session.CreatedAt.ToString("MMM dd, yyyy");
        var createdTime = session.CreatedAt.ToString("HH:mm");

        // Get message count with appropriate color coding
        var messageCount = session.History.Count;
        var messageColor = messageCount switch
        {
            0 => "dim",
            < 5 => "yellow",
            < 20 => "green",
            _ => "blue"
        };

        // Calculate how recent the session is
        var daysSinceCreated = (DateTime.Now - session.CreatedAt).Days;
        var ageText = daysSinceCreated switch
        {
            0 => "[green]Today[/]",
            1 => "[yellow]Yesterday[/]",
            < 7 => $"[orange3]{daysSinceCreated} days ago[/]",
            < 30 => $"[grey]{daysSinceCreated} days ago[/]",
            _ => $"[dim]{createdDate}[/]"
        };

        // Format message count with better spacing
        var messageText = messageCount == 0
            ? $"[dim]No messages[/]"
            : $"[{messageColor}]{messageCount} message{(messageCount == 1 ? "" : "s")}[/]";

        // Create fixed-width columns with proper alignment
        // Age column: 15 chars, Message column: 15 chars, Time column: 8 chars
        var ageColumn = PadMarkupText(ageText, 15);
        var messageColumn = PadMarkupText(messageText, 15);
        var timeColumn = $"[grey]{createdTime}[/]";

        // Use bullet separators between columns
        var bulletSeparator = " [dim]•[/] ";

        return $"{ageColumn}{bulletSeparator}{messageColumn}{bulletSeparator}{timeColumn}";
    }

    /// <summary>
    /// Pads markup text to a specific width, accounting for markup tags that don't contribute to display width.
    /// </summary>
    private static string PadMarkupText(string markupText, int targetWidth)
    {
        // Calculate the display width by removing markup tags
        var displayText = System.Text.RegularExpressions.Regex.Replace(markupText, @"\[/?[^\]]*\]", "");
        var displayWidth = displayText.Length;

        if (displayWidth >= targetWidth)
        {
            return markupText;
        }

        // Add padding spaces to reach target width
        var paddingNeeded = targetWidth - displayWidth;
        return markupText + new string(' ', paddingNeeded);
    }

    public async Task OnSelectionAsync(string selection)
    {
        // Use cached sessions to ensure consistency with what was displayed to the user
        if (_cachedSessions == null)
        {
            _logger.LogWarning("SessionListProvider: No cached sessions available, falling back to fresh lookup");
            _cachedSessions = await _sessionManager.ListSessionsAsync();
        }

        var selectedSession = _cachedSessions.FirstOrDefault(s =>
            string.Equals(s.Name, selection, StringComparison.OrdinalIgnoreCase));

        if (selectedSession != null)
        {
            _logger.LogInformation("SessionListProvider: Loading session '{SessionName}' (ID: {SessionId})",
                selectedSession.Name, selectedSession.Id);

            // Load the session data
            await _sessionManager.LoadSessionAsync(selectedSession.Id.ToString());

            // Clear existing history before loading new session
            _historyManager.ClearHistory();

            // Load session messages into history manager for UI display (same logic as FlexColumnTuiApp.Initialize)
            var sessionMessages = _sessionManager.GetCurrentSessionMessages();
            foreach (var message in sessionMessages)
            {
                // Add messages according to their actual role, but skip persistence since they're already persisted
                if (message.Role == ChatRole.User)
                {
                    _historyManager.AddUserMessageWithoutPersistence(message);
                }
                else if (message.Role == ChatRole.Assistant)
                {
                    _historyManager.AddAssistantMessageWithoutPersistence(message);
                }
                else if (message.Role == ChatRole.Tool)
                {
                    _historyManager.AddToolMessageWithoutPersistence(message);
                }
            }

            // Clear scrollback and render the loaded session
            _scrollbackTerminal.Initialize();
            RenderWelcomeAndHistory();

            _logger.LogInformation("SessionListProvider: Successfully loaded and rendered {MessageCount} messages from session '{SessionName}'",
                sessionMessages.Count, selectedSession.Name);
        }
        else
        {
            _logger.LogWarning("SessionListProvider: No session found with name '{SelectedSessionName}'", selection);
        }

        // Clear the cache after use
        _cachedSessions = null;
    }

    /// <summary>
    /// Renders the welcome message and chat history after loading a session.
    /// This replicates the logic from FlexColumnTuiApp.RenderInitialContent().
    /// </summary>
    private void RenderWelcomeAndHistory()
    {
        try
        {
            // Get required services for rendering
            var tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
            var stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
            var renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();
            var themeInfo = _serviceProvider.GetRequiredService<IThemeInfo>();

            // Create render context
            var renderContext = new RenderContext(
                tuiContext,
                stateManager.CurrentStateType,
                _logger,
                _serviceProvider,
                renderingUtilities,
                themeInfo
            );

            // Render welcome panel first
            var welcomePanel = _serviceProvider.GetRequiredService<WelcomePanel>();
            var welcomeContent = welcomePanel.Render(renderContext);
            _scrollbackTerminal.WriteStatic(welcomeContent);
            _scrollbackTerminal.WriteStatic(new Markup(""));

            // If we have existing chat history, render it after the welcome message
            var chatHistory = _historyManager.GetCurrentChatHistory();
            if (chatHistory.Any())
            {
                _scrollbackTerminal.WriteStatic(new Markup($"[dim]Loading {chatHistory.Count} messages from existing chat history[/]"));
                _scrollbackTerminal.WriteStatic(new Markup(""));
                RenderHistory(renderingUtilities);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering welcome and history for loaded session");
        }
    }

    /// <summary>
    /// Renders the chat history messages to the scrollback terminal.
    /// This replicates the logic from FlexColumnTuiApp.RenderHistory().
    /// </summary>
    private void RenderHistory(IRenderingUtilities renderingUtilities)
    {
        var chatHistory = _historyManager.GetCurrentChatHistory();
        ChatRole? previousRole = null;

        foreach (var message in chatHistory)
        {
            if (previousRole != message.Role)
            {
                _scrollbackTerminal.WriteStatic(new Markup(""));
            }

            var renderedMessage = renderingUtilities.RenderMessage(message);
            _scrollbackTerminal.WriteStatic(renderedMessage);

            if (previousRole != message.Role)
            {
                _scrollbackTerminal.WriteStatic(new Markup(""));
            }

            previousRole = message.Role;
        }
    }
}
