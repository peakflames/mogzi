namespace Mogzi.TUI.Services;

/// <summary>
/// Provides user selection functionality for listing and selecting sessions.
/// Implements the IUserSelectionProvider interface to integrate with the TUI's user selection system.
/// </summary>
public class SessionListProvider(SessionManager sessionManager, ChatClient chatClient, ILogger<SessionListProvider> logger) : IUserSelectionProvider
{
    private readonly SessionManager _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
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
            await _sessionManager.LoadSessionAsync(selectedSession.Id.ToString());
        }
        else
        {
            _logger.LogWarning("SessionListProvider: No session found with name '{SelectedSessionName}'", selection);
        }

        // Clear the cache after use
        _cachedSessions = null;
    }
}
