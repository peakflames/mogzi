namespace MaxBot.TUI;

/// <summary>
/// Event published when API call status changes
/// </summary>
public class ApiStatusChangedEvent : ITuiEvent
{
    /// <summary>
    /// Current token count
    /// </summary>
    public int TokenCount { get; }
    
    /// <summary>
    /// Elapsed time since API call started
    /// </summary>
    public TimeSpan ElapsedTime { get; }
    
    /// <summary>
    /// Whether the API call is completed
    /// </summary>
    public bool IsCompleted { get; }
    
    /// <summary>
    /// Unique identifier for this API call
    /// </summary>
    public string ApiCallId { get; }
    
    /// <summary>
    /// Timestamp when the status changed
    /// </summary>
    public DateTime Timestamp { get; }

    public ApiStatusChangedEvent(string apiCallId, int tokenCount, TimeSpan elapsedTime, bool isCompleted = false)
    {
        ApiCallId = apiCallId;
        TokenCount = tokenCount;
        ElapsedTime = elapsedTime;
        IsCompleted = isCompleted;
        Timestamp = DateTime.UtcNow;
    }
}
