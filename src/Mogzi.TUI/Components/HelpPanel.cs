namespace Mogzi.TUI.Components;

/// <summary>
/// Displays help information with available commands and usage tips.
/// Shows a formatted table of commands and their descriptions.
/// </summary>
/// <remarks>
/// Initializes a new instance of HelpPanel.
/// </remarks>
/// <param name="commands">Dictionary of command names and descriptions</param>
public class HelpPanel(IReadOnlyDictionary<string, string> commands) : ITuiComponent
{
    private readonly IReadOnlyDictionary<string, string> _commands = commands ?? throw new ArgumentNullException(nameof(commands));

    public string Name => "HelpPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var contentItems = new List<IRenderable>();

        // Commands table
        var table = new Table();
        _ = table.AddColumn(new TableColumn("Command").LeftAligned());
        _ = table.AddColumn("Description");
        table.Border = TableBorder.Rounded;
        _ = table.BorderColor(Color.Blue);

        foreach (var cmd in _commands.OrderBy(c => c.Key))
        {
            _ = table.AddRow(
                new Markup($"[blue]{cmd.Key}[/]"),
                new Markup(cmd.Value)
            );
        }

        var commandsPanel = new Panel(table)
            .Header(new PanelHeader(" Available Commands "))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(1, 0);

        contentItems.Add(commandsPanel);
        contentItems.Add(new Text(""));

        // Usage tips
        var tips = new Panel(new Rows(
            new Markup("[yellow]Tips:[/]"),
            new Markup("• Type [blue]/[/] and press Tab for command suggestions"),
            new Markup("• Use [blue]Ctrl+C[/] to exit at any time"),
            new Markup("• Use [blue]Ctrl+L[/] to clear the screen")
        ))
        .Header(" Usage Tips ")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Yellow)
        .Padding(1, 0);

        contentItems.Add(tips);

        return new Rows(contentItems);
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Help panel doesn't handle input events
        return Task.FromResult(false);
    }

    public Task InitializeAsync(IRenderContext context)
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
