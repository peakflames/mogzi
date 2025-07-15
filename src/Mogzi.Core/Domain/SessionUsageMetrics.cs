namespace Mogzi.Core.Domain;

/// <summary>
/// Tracks token usage metrics for a session
/// </summary>
public class SessionUsageMetrics
{
    [JsonPropertyName("inputTokens")]
    public long InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    public long OutputTokens { get; set; }

    [JsonPropertyName("cacheReadTokens")]
    public long CacheReadTokens { get; set; }

    [JsonPropertyName("cacheWriteTokens")]
    public long CacheWriteTokens { get; set; }

    [JsonPropertyName("requestCount")]
    public int RequestCount { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Total tokens for this session (input + output)
    /// </summary>
    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Adds usage data from a chat response
    /// </summary>
    /// <param name="usage">Usage details from Microsoft.Extensions.AI</param>
    public void AddUsage(UsageDetails? usage)
    {
        if (usage == null)
        {
            return;
        }

        InputTokens += usage.InputTokenCount ?? 0;
        OutputTokens += usage.OutputTokenCount ?? 0;

        // Future: Handle cache tokens when available
        // CacheReadTokens += usage.CacheReadTokenCount ?? 0;
        // CacheWriteTokens += usage.CacheWriteTokenCount ?? 0;

        RequestCount++;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Resets all usage metrics (for new sessions)
    /// </summary>
    public void Reset()
    {
        InputTokens = 0;
        OutputTokens = 0;
        CacheReadTokens = 0;
        CacheWriteTokens = 0;
        RequestCount = 0;
        LastUpdated = DateTime.UtcNow;
    }
}
