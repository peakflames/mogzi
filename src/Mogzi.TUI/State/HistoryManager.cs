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

    /// <summary>
    /// Adds a tool message without persisting to session manager.
    /// Used when loading existing session messages.
    /// </summary>
    public void AddToolMessageWithoutPersistence(ChatMessage message)
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

    /// <summary>
    /// Adds an assistant message to pending (for streaming). Does not persist until moved to completed.
    /// </summary>
    public void AddPendingAssistantMessage(ChatMessage message)
    {
        _pendingMessages.Add(message);
        NotifyStateChanged();
    }

    /// <summary>
    /// Updates the last pending message (for streaming updates). Does not persist.
    /// </summary>
    public void UpdateLastPendingMessage(ChatMessage message)
    {
        if (_pendingMessages.Any())
        {
            _pendingMessages.RemoveAt(_pendingMessages.Count - 1);
        }
        _pendingMessages.Add(message);
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

            // Persist the completed message
            _ = PersistMessageAsync(message);

            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Finalizes streaming by moving all pending messages to completed and persisting them.
    /// This consolidates streaming chunks into final messages before persistence.
    /// </summary>
    public async Task FinalizeStreamingAsync()
    {
        // Move all pending messages to completed and persist them
        var pendingToMove = _pendingMessages.ToList();
        _pendingMessages.Clear();

        foreach (var message in pendingToMove)
        {
            _completedMessages.Add(message);
            await PersistMessageAsync(message);
        }

        NotifyStateChanged();
    }

    /// <summary>
    /// Gets all messages (completed + pending) for UI display during streaming.
    /// </summary>
    public List<ChatMessage> GetAllMessagesForDisplay()
    {
        var allMessages = new List<ChatMessage>();
        allMessages.AddRange(_completedMessages);
        allMessages.AddRange(_pendingMessages);
        return allMessages;
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
        // For AI processing, include both completed and pending messages
        // This ensures the AI has full context during streaming
        var allMessages = new List<ChatMessage>();
        allMessages.AddRange(_completedMessages);
        allMessages.AddRange(_pendingMessages);
        return allMessages;
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
