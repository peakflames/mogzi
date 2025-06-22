using FluentResults;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaxBot.Services;

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
    /// Creates a new chat session
    /// </summary>
    /// <returns>The path to the new chat session</returns>
    string CreateChatSession();

    /// <summary>
    /// Loads a chat session
    /// </summary>
    /// <param name="sessionId">The session ID to load</param>
    /// <param name="systemPrompt">The system prompt to inject</param>
    /// <returns>The loaded chat history, or null if the session doesn't exist</returns>
    Task<List<ChatMessage>?> LoadChatSessionAsync(string sessionId, string systemPrompt);

    /// <summary>
    /// Gets all available chat sessions
    /// </summary>
    /// <returns>A list of chat session paths</returns>
    List<string> GetChatSessions();

    /// <summary>
    /// Gets the base path for chat sessions
    /// </summary>
    /// <returns>The base path for chat sessions</returns>
    string GetChatSessionsBasePath();

    /// <summary>
    /// Saves the chat history
    /// </summary>
    /// <param name="sessionPath">The path to the session</param>
    /// <param name="chatHistory">The chat history to save</param>
    Task SaveChatHistoryAsync(string sessionPath, List<ChatMessage> chatHistory);

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
