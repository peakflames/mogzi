namespace MaxBot.TUI;

/// <summary>
/// Event triggered when rich content (formatted text) is received for display.
/// This event carries rich content that can include Spectre.Console markup or basic markdown.
/// </summary>
public class RichContentMessageReceivedEvent : ITuiEvent
{
    public DateTime Timestamp { get; }
    public string Content { get; }
    public string? Title { get; }

    public RichContentMessageReceivedEvent(string content, string? title = null)
    {
        Timestamp = DateTime.UtcNow;
        Content = content ?? string.Empty;
        Title = title;
    }
}
