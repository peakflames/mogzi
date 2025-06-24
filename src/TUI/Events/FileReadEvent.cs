namespace MaxBot.TUI;

public class FileReadEvent : ITuiEvent
{
    public string FilePath { get; }

    public FileReadEvent(string filePath)
    {
        FilePath = filePath;
    }
}
