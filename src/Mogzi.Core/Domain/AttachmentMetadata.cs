using System.Text.Json.Serialization;

namespace Mogzi.Domain;

/// <summary>
/// Metadata for an attachment stored in the session directory
/// </summary>
public class AttachmentMetadata
{
    /// <summary>
    /// The original filename of the attachment
    /// </summary>
    [JsonPropertyName("originalFileName")]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// The stored filename in the attachments directory
    /// </summary>
    [JsonPropertyName("storedFileName")]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// The media type (MIME type) of the attachment
    /// </summary>
    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = string.Empty;

    /// <summary>
    /// The size of the attachment in bytes
    /// </summary>
    [JsonPropertyName("sizeBytes")]
    public long SizeBytes { get; set; }

    /// <summary>
    /// The hash of the attachment content for deduplication
    /// </summary>
    [JsonPropertyName("contentHash")]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// The index of the message this attachment belongs to
    /// </summary>
    [JsonPropertyName("messageIndex")]
    public int MessageIndex { get; set; }

    /// <summary>
    /// The index of the content within the message
    /// </summary>
    [JsonPropertyName("contentIndex")]
    public int ContentIndex { get; set; }
}
