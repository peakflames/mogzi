using System.Text.Json;

namespace Mogzi.Services;

/// <summary>
/// Service for managing chat history persistence to disk
/// </summary>
public class ChatHistoryService
{
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the ChatHistoryService class
    /// </summary>
    public ChatHistoryService()
    {
        // Create the base directory in the user's profile
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _basePath = Path.Combine(userProfile, ".mogzi", "chats");

        // Create the directory if it doesn't exist
        _ = Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Creates a new chat session directory with a timestamp
    /// </summary>
    /// <returns>The path to the new chat session directory</returns>
    public string CreateChatSession()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var sessionPath = Path.Combine(_basePath, timestamp);

        _ = Directory.CreateDirectory(sessionPath);

        return sessionPath;
    }

    /// <summary>
    /// Saves chat history to disk
    /// </summary>
    /// <param name="sessionPath">The path to the chat session directory</param>
    /// <param name="chatHistory">The chat history to save</param>
    public async Task SaveChatHistoryAsync(string sessionPath, List<ChatMessage> chatHistory)
    {
        try
        {
            // Filter out System role messages as per requirement
            var filteredHistory = chatHistory.Where(m => m.Role != ChatRole.System).ToList();

            // Convert the chat history to a serializable format
            var historyRoot = new ChatHistoryRoot
            {
                Messages = [.. filteredHistory.Select(SerializableChatMessage.FromChatMessage)],
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                LastUpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // Check if the file already exists to preserve the creation date
            var filePath = Path.Combine(sessionPath, "chatHistory.json");
            if (File.Exists(filePath))
            {
                try
                {
                    using FileStream readFs = new(filePath, FileMode.Open);
                    var existingRoot = await JsonSerializer.DeserializeAsync(readFs, ChatHistoryContext.Default.ChatHistoryRoot);
                    if (existingRoot != null && !string.IsNullOrEmpty(existingRoot.CreatedAt))
                    {
                        historyRoot.CreatedAt = existingRoot.CreatedAt;
                    }
                }
                catch
                {
                    // If there's an error reading the existing file, just use the current timestamp
                }
            }

            // Save the chat history to disk
            using FileStream fs = new(filePath, FileMode.Create);
            await JsonSerializer.SerializeAsync(fs, historyRoot, ChatHistoryContext.Default.ChatHistoryRoot);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving chat history: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Loads chat history from disk
    /// </summary>
    /// <param name="sessionPath">The path to the chat session directory</param>
    /// <param name="systemPrompt">The system prompt to inject as the first message</param>
    /// <returns>The loaded chat history, or null if the file doesn't exist</returns>
    public async Task<List<ChatMessage>?> LoadChatHistoryAsync(string sessionPath, string? systemPrompt = null)
    {
        var filePath = Path.Combine(sessionPath, "chatHistory.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using FileStream fs = new(filePath, FileMode.Open);
            var historyRoot = await JsonSerializer.DeserializeAsync(fs, ChatHistoryContext.Default.ChatHistoryRoot);

            if (historyRoot == null)
            {
                return null;
            }

            // Convert the serializable format back to ChatMessage
            var messages = historyRoot.Messages.Select(m => m.ToChatMessage()).ToList();

            // If a system prompt is provided, inject it as the first message
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
            }

            return messages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading chat history: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets a list of all chat sessions
    /// </summary>
    /// <returns>A list of chat session directories</returns>
    public List<string> GetChatSessions()
    {
        if (!Directory.Exists(_basePath))
        {
            return [];
        }

        return [.. Directory.GetDirectories(_basePath).OrderByDescending(d => d)];
    }

    /// <summary>
    /// Gets the most recent chat session, or null if none exists
    /// </summary>
    /// <returns>The path to the most recent chat session, or null if none exists</returns>
    public string? GetMostRecentChatSession()
    {
        var sessions = GetChatSessions();
        return sessions.Count > 0 ? sessions[0] : null;
    }

    /// <summary>
    /// Gets the base path for chat history storage
    /// </summary>
    /// <returns>The base path for chat history storage</returns>
    public string GetBasePath()
    {
        return _basePath;
    }
}
