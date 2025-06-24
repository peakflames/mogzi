namespace MaxBot.TUI;

public class ConsoleRenderer
{
    private readonly IAnsiConsole _console;
    private readonly TuiEventBus _eventBus;
    private readonly List<ITuiCard> _cards = new();

    public ConsoleRenderer(IAnsiConsole console, TuiEventBus eventBus)
    {
        _console = console;
        _eventBus = eventBus;
        RegisterEventHandlers();
    }

    private void RegisterEventHandlers()
    {
        _eventBus.Register<FileReadEvent>(OnFileRead);
        _eventBus.Register<FilePatchedEvent>(OnFilePatched);
    }

    private Task OnFileRead(FileReadEvent e)
    {
        _cards.Add(new FileCard(e.FilePath));
        Render();
        return Task.CompletedTask;
    }

    private Task OnFilePatched(FilePatchedEvent e)
    {
        _cards.Add(new DiffCard(e.FilePath, e.Diff));
        Render();
        return Task.CompletedTask;
    }

    private void Render()
    {
        _console.Clear();
        foreach (var card in _cards)
        {
            _console.Write(card.Render());
        }
    }

    public static void ConsoleWriteLLMResponseDetails(string response, ConsoleColor color = ConsoleColor.DarkGray)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"\n{response}");
        Console.ForegroundColor = originalColor;
    }

    public static void ConsoleWriteError(string message)
    {
        var temp = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ForegroundColor = temp;
    }
}
