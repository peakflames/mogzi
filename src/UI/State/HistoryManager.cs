using MaxBot;
using System.Collections.Generic;
using System.Linq;

namespace UI.State;

public class HistoryManager
{
    private readonly List<ChatMessage> _completedMessages = new();
    private readonly List<ChatMessage> _pendingMessages = new();

    public void AddUserMessage(ChatMessage message)
    {
        _completedMessages.Add(message);
    }

    public void AddAssistantMessage(ChatMessage message)
    {
        _completedMessages.Add(message);
    }

    public List<ChatMessage> GetCompletedMessages()
    {
        return _completedMessages.ToList();
    }

    public List<ChatMessage> GetCurrentChatHistory()
    {
        // In a real implementation, this would be more complex,
        // including pending messages, system prompts, etc.
        return _completedMessages.ToList();
    }
}
