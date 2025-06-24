namespace MaxBot.TUI;

public class FileCard : ITuiCard
{
    private readonly string _filePath;

    public FileCard(string filePath)
    {
        _filePath = filePath;
    }

    public IRenderable Render()
    {
        return new Panel(new Text(_filePath))
            .Header("File Read")
            .Border(BoxBorder.Rounded);
    }
}
