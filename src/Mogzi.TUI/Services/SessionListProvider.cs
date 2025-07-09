namespace Mogzi.TUI.Services;

/// <summary>
/// Provides user selection functionality for listing and selecting sessions.
/// Implements the IUserSelectionProvider interface to integrate with the TUI's user selection system.
/// </summary>
public class SessionListProvider(SessionManager sessionManager, ChatClient chatClient) : IUserSelectionProvider
{
    private readonly SessionManager _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    private readonly ChatClient _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));

    public string Command => "/session list";
    public string Description => "List and select from available sessions.";

    public async Task<List<CompletionItem>> GetSelectionsAsync()
    {
        var sessions = await _sessionManager.ListSessionsAsync();

        var selections = new List<CompletionItem>();

        // Limit to the configured number of sessions (default 10) for better UX
        var sessionLimit = _chatClient.Config.SessionListLimit;
        var recentSessions = sessions.Take(sessionLimit);

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
        // Find the session by name and load it
        var sessions = await _sessionManager.ListSessionsAsync();
        var selectedSession = sessions.FirstOrDefault(s =>
            string.Equals(s.Name, selection, StringComparison.OrdinalIgnoreCase));

        if (selectedSession != null)
        {
            await _sessionManager.LoadSessionAsync(selectedSession.Id.ToString());
        }
    }
}
