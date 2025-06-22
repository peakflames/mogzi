using FluentResults;
using MaxBot.Domain;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaxBot.Services;

/// <summary>
/// Implementation of the core application service
/// </summary>
public class AppService : IAppService
{
    private readonly ChatClient _chatClient;
    private readonly ChatHistoryService _chatHistoryService;

    /// <summary>
    /// Gets the system prompt
    /// </summary>
    public string SystemPrompt => _chatClient.SystemPrompt;

    /// <summary>
    /// Gets the chat options
    /// </summary>
    public ChatOptions ChatOptions => _chatClient.ChatOptions;
    
    /// <summary>
    /// Gets the chat client
    /// </summary>
    public ChatClient ChatClient => _chatClient;

    /// <summary>
    /// Initializes a new instance of the AppService class
    /// </summary>
    /// <param name="chatClient">The chat client</param>
    public AppService(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _chatHistoryService = new ChatHistoryService();
    }

    /// <summary>
    /// Creates a new chat session
    /// </summary>
    /// <returns>The path to the new chat session</returns>
    public string CreateChatSession()
    {
        return _chatHistoryService.CreateChatSession();
    }

    /// <summary>
    /// Loads a chat session
    /// </summary>
    /// <param name="sessionId">The session ID to load</param>
    /// <param name="systemPrompt">The system prompt to inject</param>
    /// <returns>The loaded chat history, or null if the session doesn't exist</returns>
    public async Task<List<ChatMessage>?> LoadChatSessionAsync(string sessionId, string systemPrompt)
    {
        string sessionPath = Path.Combine(_chatHistoryService.GetBasePath(), sessionId);
        if (!Directory.Exists(sessionPath))
        {
            return null;
        }

        return await _chatHistoryService.LoadChatHistoryAsync(sessionPath, systemPrompt);
    }

    /// <summary>
    /// Gets all available chat sessions
    /// </summary>
    /// <returns>A list of chat session paths</returns>
    public List<string> GetChatSessions()
    {
        return _chatHistoryService.GetChatSessions();
    }

    /// <summary>
    /// Gets the base path for chat sessions
    /// </summary>
    /// <returns>The base path for chat sessions</returns>
    public string GetChatSessionsBasePath()
    {
        return _chatHistoryService.GetBasePath();
    }

    /// <summary>
    /// Saves the chat history
    /// </summary>
    /// <param name="sessionPath">The path to the session</param>
    /// <param name="chatHistory">The chat history to save</param>
    public async Task SaveChatHistoryAsync(string sessionPath, List<ChatMessage> chatHistory)
    {
        await _chatHistoryService.SaveChatHistoryAsync(sessionPath, chatHistory);
    }

    /// <summary>
    /// Processes a chat message
    /// </summary>
    /// <param name="chatHistory">The current chat history</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>An async enumerable of chat response updates</returns>
    public IAsyncEnumerable<ChatResponseUpdate> ProcessChatMessageAsync(List<ChatMessage> chatHistory, CancellationToken cancellationToken)
    {
        // Ensure the first message is the system prompt with the current value
        if (chatHistory.Count > 0 && chatHistory[0].Role == ChatRole.System)
        {
            // Update the system message with the current system prompt
            chatHistory[0] = new ChatMessage(ChatRole.System, _chatClient.SystemPrompt);
        }
        
        return _chatClient.ChatClientMEAI.GetStreamingResponseAsync(chatHistory, _chatClient.ChatOptions, cancellationToken);
    }

    /// <summary>
    /// Calculates token metrics for the chat history
    /// </summary>
    /// <param name="chatHistory">The chat history</param>
    /// <returns>The token count</returns>
    public int CalculateTokenMetrics(List<ChatMessage> chatHistory)
    {
        return MaxBot.Utils.ApiMetricUtils.GetSimplisticTokenCount(chatHistory);
    }
}
