using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Mogzi.Domain;

/// <summary>
/// Serializable representation of a chat message with attachment support
/// </summary>
public class SerializableChatMessage
{
    /// <summary>
    /// The role of the message sender
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// The text content of the message
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
    /// Metadata for attachments associated with this message
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<AttachmentMetadata> Attachments { get; set; } = [];

    /// <summary>
    /// Creates a SerializableChatMessage from a ChatMessage, extracting attachments
    /// </summary>
    public static SerializableChatMessage FromChatMessage(ChatMessage message, int messageIndex, string attachmentsDirectory)
    {
        var serializable = new SerializableChatMessage
        {
            Role = message.Role.ToString().ToLower(),
            Content = message.Text,
            AuthorName = message.AuthorName,
            MessageId = message.MessageId
        };

        // Process attachments from message contents
        for (var contentIndex = 0; contentIndex < message.Contents.Count; contentIndex++)
        {
            var content = message.Contents[contentIndex];

            if (content is DataContent dataContent)
            {
                var attachment = ProcessDataContent(dataContent, messageIndex, contentIndex, attachmentsDirectory);
                if (attachment != null)
                {
                    serializable.Attachments.Add(attachment);
                }
            }
            else if (content is UriContent uriContent)
            {
                var attachment = ProcessUriContent(uriContent, messageIndex, contentIndex);
                if (attachment != null)
                {
                    serializable.Attachments.Add(attachment);
                }
            }
        }

        return serializable;
    }

    /// <summary>
    /// Converts this SerializableChatMessage to a ChatMessage, restoring attachments
    /// </summary>
    public ChatMessage ToChatMessage(string attachmentsDirectory)
    {
        var role = ParseRole(Role);
        var contents = new List<AIContent>();

        // Add text content if present
        if (!string.IsNullOrEmpty(Content))
        {
            contents.Add(new TextContent(Content));
        }

        // Restore attachments
        foreach (var attachment in Attachments)
        {
            var attachmentPath = Path.Combine(attachmentsDirectory, attachment.StoredFileName);
            if (File.Exists(attachmentPath))
            {
                try
                {
                    var data = File.ReadAllBytes(attachmentPath);
                    var dataContent = new DataContent(data, attachment.MediaType);
                    contents.Add(dataContent);
                }
                catch (Exception ex)
                {
                    // Log error and add error content instead
                    contents.Add(new ErrorContent($"Failed to load attachment {attachment.OriginalFileName}: {ex.Message}"));
                }
            }
            else
            {
                contents.Add(new ErrorContent($"Attachment not found: {attachment.OriginalFileName}"));
            }
        }

        var message = new ChatMessage(role, contents)
        {
            AuthorName = AuthorName,
            MessageId = MessageId
        };

        return message;
    }

    private static ChatRole ParseRole(string role)
    {
        return role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "user" => ChatRole.User,
            "tool" => ChatRole.Tool,
            _ => ChatRole.User
        };
    }

    private static AttachmentMetadata? ProcessDataContent(DataContent dataContent, int messageIndex, int contentIndex, string attachmentsDirectory)
    {
        try
        {
            var data = dataContent.Data;
            if (data.IsEmpty)
            {
                return null;
            }

            // Generate content hash for deduplication
            var contentHash = ComputeHash(data.Span);

            // Generate filename based on media type
            var extension = GetExtensionFromMediaType(dataContent.MediaType);
            var storedFileName = $"{messageIndex}-{contentIndex}-{contentHash}{extension}";

            // Ensure attachments directory exists
            _ = Directory.CreateDirectory(attachmentsDirectory);

            // Save the file
            var filePath = Path.Combine(attachmentsDirectory, storedFileName);
            File.WriteAllBytes(filePath, data.ToArray());

            return new AttachmentMetadata
            {
                OriginalFileName = $"attachment{extension}",
                StoredFileName = storedFileName,
                MediaType = dataContent.MediaType,
                SizeBytes = data.Length,
                ContentHash = contentHash,
                MessageIndex = messageIndex,
                ContentIndex = contentIndex
            };
        }
        catch (Exception)
        {
            // If we can't process the attachment, skip it
            return null;
        }
    }

    private static AttachmentMetadata? ProcessUriContent(UriContent uriContent, int messageIndex, int contentIndex)
    {
        // For URI content, we just store the metadata without downloading
        // The URI will be preserved in the attachment metadata
        return new AttachmentMetadata
        {
            OriginalFileName = Path.GetFileName(uriContent.Uri.ToString()) ?? "uri-content",
            StoredFileName = uriContent.Uri.ToString(), // Store URI as filename for URI content
            MediaType = uriContent.MediaType,
            SizeBytes = 0, // Unknown size for URI content
            ContentHash = ComputeHash(Encoding.UTF8.GetBytes(uriContent.Uri.ToString())),
            MessageIndex = messageIndex,
            ContentIndex = contentIndex
        };
    }

    private static string ComputeHash(ReadOnlySpan<byte> data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash)[..16]; // Use first 16 characters for shorter filenames
    }

    private static string GetExtensionFromMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/tiff" => ".tiff",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "text/html" => ".html",
            "text/css" => ".css",
            "text/javascript" => ".js",
            "application/json" => ".json",
            "application/xml" => ".xml",
            "audio/mpeg" => ".mp3",
            "audio/wav" => ".wav",
            "audio/ogg" => ".ogg",
            "video/mp4" => ".mp4",
            "video/webm" => ".webm",
            _ => ".bin" // Default for unknown types
        };
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
    public List<SerializableChatMessage> Messages { get; set; } = [];

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
