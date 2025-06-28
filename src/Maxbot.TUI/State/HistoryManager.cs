namespace Maxbot.TUI.State;

public class HistoryManager
{
    private readonly List<ChatMessage> _completedMessages = new();
    private readonly List<ChatMessage> _pendingMessages = new();
    private readonly StateManager _stateManager;

    /// <summary>
    /// Unique identifier for this HistoryManager instance for state change notifications.
    /// </summary>
    public string ComponentId { get; } = "HistoryManager";

    public HistoryManager(StateManager stateManager)
    {
        _stateManager = stateManager;
    }

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
        return _completedMessages.ToList();
    }

    public List<ChatMessage> GetPendingMessages()
    {
        return _pendingMessages.ToList();
    }

    public List<ChatMessage> GetCurrentChatHistory()
    {
        // In a real implementation, this would be more complex,
        // including pending messages, system prompts, etc.
        return _completedMessages.ToList();
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
