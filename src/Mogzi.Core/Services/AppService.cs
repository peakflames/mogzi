namespace Mogzi.Services;

/// <summary>
/// Implementation of the core application service
/// </summary>
/// <remarks>
/// Initializes a new instance of the AppService class
/// </remarks>
/// <param name="chatClient">The chat client</param>
public class AppService(ChatClient chatClient) : IAppService
{
    /// <summary>
    /// Gets the system prompt
    /// </summary>
    public string SystemPrompt => ChatClient.SystemPrompt;

    /// <summary>
    /// Gets the chat options
    /// </summary>
    public ChatOptions ChatOptions => ChatClient.ChatOptions;

    /// <summary>
    /// Gets the chat client
    /// </summary>
    public ChatClient ChatClient { get; } = chatClient;

    /// <summary>
    /// Processes a chat message
    /// </summary>
    /// <param name="chatHistory">The current chat history</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An async enumerable of chat response updates</returns>
    public IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken)
    {
        // Ensure the first message is the system prompt with the current value
        if (chatHistory.Count == 0 || chatHistory[0].Role != ChatRole.System)
        {
            chatHistory.Insert(0, new ChatMessage(ChatRole.System, ChatClient.SystemPrompt));
        }
        else
        {
            // Update the system message with the current system prompt
            chatHistory[0] = new ChatMessage(ChatRole.System, ChatClient.SystemPrompt);
        }

        return ChatClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory, ChatClient.ChatOptions, cancellationToken);
    }

    /// <summary>
    /// Calculates token metrics for the chat history
    /// </summary>
    /// <param name="chatHistory">The chat history</param>
    /// <returns>The token count</returns>
    public int CalculateTokenMetrics(List<ChatMessage> chatHistory)
    {
        return Utils.ApiMetricUtils.GetSimplisticTokenCount(chatHistory);
    }
}
