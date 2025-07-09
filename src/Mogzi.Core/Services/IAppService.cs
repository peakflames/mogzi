namespace Mogzi.Services;

/// <summary>
/// Interface for the core application service
/// </summary>
public interface IAppService
{
    /// <summary>
    /// Gets the system prompt
    /// </summary>
    string SystemPrompt { get; }

    /// <summary>
    /// Gets the chat options
    /// </summary>
    ChatOptions ChatOptions { get; }

    /// <summary>
    /// Gets the chat client
    /// </summary>
    ChatClient ChatClient { get; }

    /// <summary>
    /// Processes a chat message
    /// </summary>
    /// <param name="chatHistory">The current chat history</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An async enumerable of chat response updates</returns>
    IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken);

    /// <summary>
    /// Calculates token metrics for the chat history
    /// </summary>
    /// <param name="chatHistory">The chat history</param>
    /// <returns>The token count</returns>
    int CalculateTokenMetrics(List<ChatMessage> chatHistory);
}
