namespace MaxBot.TUI;

public class TextReceivedEvent : ITuiEvent
{
    public string Text { get; }

    public TextReceivedEvent(string text)
    {
        Text = text;
    }
}
