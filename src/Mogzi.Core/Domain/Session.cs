namespace Mogzi.Core.Domain;

public class Session
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastModifiedAt")]
    public DateTime LastModifiedAt { get; set; }

    [JsonPropertyName("history")]
    public List<SerializableChatMessage> History { get; set; } = [];

    [JsonPropertyName("initialPrompt")]
    public string InitialPrompt { get; set; } = string.Empty;

    [JsonPropertyName("usageMetrics")]
    public SessionUsageMetrics? UsageMetrics { get; set; }

    [JsonPropertyName("isFirstMessage")]
    public bool IsFirstMessage { get; set; } = true;
}

/// <summary>
/// JSON serialization context for session management
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(List<Session>))]
[JsonSerializable(typeof(SerializableChatMessage))]
[JsonSerializable(typeof(List<SerializableChatMessage>))]
[JsonSerializable(typeof(AttachmentMetadata))]
[JsonSerializable(typeof(List<AttachmentMetadata>))]
[JsonSerializable(typeof(SessionUsageMetrics))]
public partial class SessionContext : JsonSerializerContext
{
}
