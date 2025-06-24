namespace MaxBot.TUI;

public class FilePatchedEvent : ITuiEvent
{
    public string FilePath { get; }
    public string Diff { get; }

    public FilePatchedEvent(string filePath, string diff)
    {
        FilePath = filePath;
        Diff = diff;
    }
}
