using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace MaxBot.Domain;

/// <summary>
/// Serializable representation of a chat message
/// </summary>
public class SerializableChatMessage
{
    /// <summary>
    /// The role of the message sender
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// The content of the message
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the author (optional)
    /// </summary>
    [JsonPropertyName("authorName")]
    public string? AuthorName { get; set; }
    
    /// <summary>
    /// The message ID (optional)
    /// </summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; set; }
    
    /// <summary>
    /// Creates a SerializableChatMessage from a ChatMessage
    /// </summary>
    public static SerializableChatMessage FromChatMessage(ChatMessage message)
    {
        return new SerializableChatMessage
        {
            Role = message.Role.ToString().ToLower(),
            Content = message.Text
        };
    }
    
    /// <summary>
    /// Converts this SerializableChatMessage to a ChatMessage
    /// </summary>
    public ChatMessage ToChatMessage()
    {
        ChatRole role = ChatRole.User;
        
        if (Role.Equals("system", StringComparison.OrdinalIgnoreCase))
        {
            role = ChatRole.System;
        }
        else if (Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
        {
            role = ChatRole.Assistant;
        }
        else if (Role.Equals("user", StringComparison.OrdinalIgnoreCase))
        {
            role = ChatRole.User;
        }
        
        return new ChatMessage(role, Content);
    }
}

/// <summary>
/// Root object for chat history serialization
/// </summary>
public class ChatHistoryRoot
{
    /// <summary>
    /// The list of chat messages
    /// </summary>
    [JsonPropertyName("messages")]
    public List<SerializableChatMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// The timestamp when the chat history was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
    
    /// <summary>
    /// The timestamp when the chat history was last updated
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public string LastUpdatedAt { get; set; } = string.Empty;
}

/// <summary>
/// JSON serialization context for chat history
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ChatHistoryRoot))]
[JsonSerializable(typeof(SerializableChatMessage))]
[JsonSerializable(typeof(List<SerializableChatMessage>))]
public partial class ChatHistoryContext : JsonSerializerContext
{
}
