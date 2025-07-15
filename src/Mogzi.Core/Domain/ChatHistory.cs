namespace Mogzi.Domain;

/// <summary>
/// Serializable representation of a function call
/// </summary>
public class SerializableFunctionCall
{
    /// <summary>
    /// The unique identifier for this function call
    /// </summary>
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the function being called
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The arguments passed to the function as JSON
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;
}

/// <summary>
/// Serializable representation of a function result
/// </summary>
public class SerializableFunctionResult
{
    /// <summary>
    /// The unique identifier for the function call this result corresponds to
    /// </summary>
    [JsonPropertyName("callId")]
    public string CallId { get; set; } = string.Empty;

    /// <summary>
    /// The result of the function execution
    /// </summary>
    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}

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
    /// Serialized function calls for tool execution messages
    /// </summary>
    [JsonPropertyName("functionCalls")]
    public List<SerializableFunctionCall> FunctionCalls { get; set; } = [];

    /// <summary>
    /// Serialized function results for tool execution messages
    /// </summary>
    [JsonPropertyName("functionResults")]
    public List<SerializableFunctionResult> FunctionResults { get; set; } = [];

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

        // Process all content types from message contents
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
            else if (content is FunctionCallContent functionCall)
            {
                var serializableFunctionCall = new SerializableFunctionCall
                {
                    CallId = functionCall.CallId,
                    Name = functionCall.Name ?? string.Empty,
                    Arguments = SerializeArgumentsManually(functionCall.Arguments)
                };
                serializable.FunctionCalls.Add(serializableFunctionCall);
            }
            else if (content is FunctionResultContent functionResult)
            {
                var serializableFunctionResult = new SerializableFunctionResult
                {
                    CallId = functionResult.CallId,
                    Result = functionResult.Result?.ToString() ?? string.Empty
                };
                serializable.FunctionResults.Add(serializableFunctionResult);
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

        // Restore function calls
        foreach (var functionCall in FunctionCalls)
        {
            var arguments = DeserializeArgumentsManually(functionCall.Arguments);
            var functionCallContent = new FunctionCallContent(functionCall.CallId, functionCall.Name, arguments);
            contents.Add(functionCallContent);
        }

        // Restore function results
        foreach (var functionResult in FunctionResults)
        {
            var functionResultContent = new FunctionResultContent(functionResult.CallId, functionResult.Result);
            contents.Add(functionResultContent);
        }

        // CRITICAL: Set role based on content type
        // If this message contains function results, it should be a Tool message regardless of stored role
        if (FunctionResults.Count > 0 && FunctionCalls.Count == 0)
        {
            role = ChatRole.Tool;
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

    /// <summary>
    /// Manually serializes function arguments to JSON string for AOT compatibility.
    /// </summary>
    private static string SerializeArgumentsManually(IDictionary<string, object?>? arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            return "{}";
        }

        var sb = new StringBuilder();
        _ = sb.Append('{');

        var first = true;
        foreach (var kvp in arguments)
        {
            if (!first)
            {
                _ = sb.Append(',');
            }
            first = false;

            // Escape the key
            _ = sb.Append('"');
            _ = sb.Append(EscapeJsonString(kvp.Key));
            _ = sb.Append("\":");

            // Serialize the value
            SerializeValue(sb, kvp.Value);
        }

        _ = sb.Append('}');
        return sb.ToString();
    }

    /// <summary>
    /// Manually deserializes function arguments from JSON string for AOT compatibility.
    /// Returns a simple dictionary with string values for compatibility.
    /// </summary>
    private static IDictionary<string, object?> DeserializeArgumentsManually(string argumentsJson)
    {
        var result = new Dictionary<string, object?>();

        if (string.IsNullOrWhiteSpace(argumentsJson) || argumentsJson == "{}")
        {
            return result;
        }

        try
        {
            // Simple JSON parsing - this is a basic implementation for common cases
            // For more complex scenarios, we might need a more robust parser
            var json = argumentsJson.Trim();
            if (!json.StartsWith('{') || !json.EndsWith('}'))
            {
                return result;
            }

            json = json[1..^1]; // Remove { and }
            if (string.IsNullOrWhiteSpace(json))
            {
                return result;
            }

            // Split by commas (simple approach - doesn't handle nested objects)
            var pairs = json.Split(',');
            foreach (var pair in pairs)
            {
                var colonIndex = pair.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = pair[..colonIndex].Trim().Trim('"');
                    var value = pair[(colonIndex + 1)..].Trim();

                    // Remove quotes if it's a string value
                    if (value.StartsWith('"') && value.EndsWith('"'))
                    {
                        value = value[1..^1];
                    }

                    result[key] = value;
                }
            }
        }
        catch
        {
            // If parsing fails, return empty dictionary
            // This ensures the system doesn't crash on malformed JSON
        }

        return result;
    }

    /// <summary>
    /// Serializes a value to JSON format manually.
    /// </summary>
    private static void SerializeValue(StringBuilder sb, object? value)
    {
        if (value == null)
        {
            _ = sb.Append("null");
        }
        else if (value is string str)
        {
            _ = sb.Append('"');
            _ = sb.Append(EscapeJsonString(str));
            _ = sb.Append('"');
        }
        else if (value is bool boolValue)
        {
            _ = sb.Append(boolValue ? "true" : "false");
        }
        else if (value is int or long or float or double or decimal)
        {
            _ = sb.Append(value.ToString());
        }
        else
        {
            // For other types, convert to string and quote
            _ = sb.Append('"');
            _ = sb.Append(EscapeJsonString(value.ToString() ?? ""));
            _ = sb.Append('"');
        }
    }

    /// <summary>
    /// Escapes a string for JSON format.
    /// </summary>
    private static string EscapeJsonString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sb = new StringBuilder();
        foreach (var c in input)
        {
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (c)
            {
                case '"':
                    _ = sb.Append("\\\"");
                    break;
                case '\\':
                    _ = sb.Append("\\\\");
                    break;
                case '\b':
                    _ = sb.Append("\\b");
                    break;
                case '\f':
                    _ = sb.Append("\\f");
                    break;
                case '\n':
                    _ = sb.Append("\\n");
                    break;
                case '\r':
                    _ = sb.Append("\\r");
                    break;
                case '\t':
                    _ = sb.Append("\\t");
                    break;
                default:
                    _ = c < ' ' ? sb.Append($"\\u{(int)c:x4}") : sb.Append(c);
                    break;
            }
#pragma warning restore IDE0066 // Convert switch statement to expression
        }
        return sb.ToString();
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
[JsonSerializable(typeof(SerializableFunctionCall))]
[JsonSerializable(typeof(SerializableFunctionResult))]
[JsonSerializable(typeof(List<SerializableFunctionCall>))]
[JsonSerializable(typeof(List<SerializableFunctionResult>))]
public partial class ChatHistoryContext : JsonSerializerContext
{
}
