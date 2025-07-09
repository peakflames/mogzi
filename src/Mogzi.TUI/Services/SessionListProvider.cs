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
            var description = $"Created: {session.CreatedAt:yyyy-MM-dd HH:mm} | Messages: {session.History.Count}";

            selections.Add(new CompletionItem(displayName, description));
        }

        return selections;
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
