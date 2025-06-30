namespace MaxBot.TUI.State;

public class HistoryManager(StateManager stateManager)
{
    private readonly List<ChatMessage> _completedMessages = [];
    private readonly List<ChatMessage> _pendingMessages = [];
    private readonly StateManager _stateManager = stateManager;

    /// <summary>
    /// Unique identifier for this HistoryManager instance for state change notifications.
    /// </summary>
    public string ComponentId { get; } = "HistoryManager";

    public void AddUserMessage(ChatMessage message)
    {
        _completedMessages.Add(message);
        NotifyStateChanged();
    }

    public void AddAssistantMessage(ChatMessage message)
    {
        _completedMessages.Add(message);
        NotifyStateChanged();
    }

    public void UpdateLastMessage(ChatMessage message)
    {
        if (_completedMessages.Any())
        {
            _completedMessages.RemoveAt(_completedMessages.Count - 1);
        }
        _completedMessages.Add(message);
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
        _stateManager.NotifyStateChanged(ComponentId);
    }
}
