namespace Mogzi.TUI.State;

public class HistoryManager(ITuiMediator mediator)
{
    private readonly List<ChatMessage> _completedMessages = [];
    private readonly List<ChatMessage> _pendingMessages = [];
    private readonly ITuiMediator _mediator = mediator;
    private SessionManager? _sessionManager;

    /// <summary>
    /// Sets the session manager for persistence. This is called during initialization.
    /// </summary>
    public void SetSessionManager(SessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Adds a user message without persisting to session manager.
    /// Used when loading existing session messages.
    /// </summary>
    public void AddUserMessageWithoutPersistence(ChatMessage message)
    {
        _completedMessages.Add(message);
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds an assistant message without persisting to session manager.
    /// Used when loading existing session messages.
    /// </summary>
    public void AddAssistantMessageWithoutPersistence(ChatMessage message)
    {
        _completedMessages.Add(message);
        NotifyStateChanged();
    }

    public void AddUserMessage(ChatMessage message)
    {
        _completedMessages.Add(message);

        // Persist to session manager asynchronously (fire-and-forget)
        _ = PersistMessageAsync(message);

        NotifyStateChanged();
    }

    public void AddAssistantMessage(ChatMessage message)
    {
        _completedMessages.Add(message);

        // Persist to session manager asynchronously (fire-and-forget)
        _ = PersistMessageAsync(message);

        NotifyStateChanged();
    }

    public void UpdateLastMessage(ChatMessage message)
    {
        if (_completedMessages.Any())
        {
            _completedMessages.RemoveAt(_completedMessages.Count - 1);
        }
        _completedMessages.Add(message);

        // For message updates, we need to update the session as well
        // This handles streaming message updates where the last message is being modified
        _ = PersistMessageAsync(message);

        NotifyStateChanged();
    }

    public void AddPendingMessage(ChatMessage message)
    {
        _pendingMessages.Add(message);
        NotifyStateChanged();
    }

    public void MovePendingToCompleted(ChatMessage message)
    {
        if (_pendingMessages.Remove(message))
        {
            _completedMessages.Add(message);
            NotifyStateChanged();
        }
    }

    public List<ChatMessage> GetCompletedMessages()
    {
        return [.. _completedMessages];
    }

    public List<ChatMessage> GetPendingMessages()
    {
        return [.. _pendingMessages];
    }

    public List<ChatMessage> GetCurrentChatHistory()
    {
        // In a real implementation, this would be more complex,
        // including pending messages, system prompts, etc.
        return [.. _completedMessages];
    }

    public void ClearHistory()
    {
        _completedMessages.Clear();
        _pendingMessages.Clear();
        NotifyStateChanged();
    }

    public int GetTotalMessageCount()
    {
        return _completedMessages.Count + _pendingMessages.Count;
    }

    private void NotifyStateChanged()
    {
        // This is now handled by the mediator, which will trigger a re-render.
        // In a more advanced implementation, the mediator could notify specific components.
        _ = _mediator.NotifyHistoryChangedAsync();
    }

    /// <summary>
    /// Persists a message to the session manager asynchronously.
    /// Uses fire-and-forget pattern to avoid blocking UI operations.
    /// </summary>
    private async Task PersistMessageAsync(ChatMessage message)
    {
        if (_sessionManager == null)
        {
            return;
        }

        try
        {
            await _sessionManager.AddMessageToCurrentSessionAsync(message);
        }
        catch (Exception)
        {
            // Log error but don't throw to avoid disrupting UI flow
            // The mediator's logger isn't available here, so we silently handle failures
            // In a production system, we might want to inject a logger or use a different error handling strategy
        }
    }
}
