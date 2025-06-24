namespace MaxBot.TUI;

/// <summary>
/// TUI card for displaying command execution with status and output
/// </summary>
public class CommandCard : ITuiCard
{
    private readonly string _command;
    private CommandStatus _status;
    private string _output;

    public CommandCard(string command, CommandStatus status, string output)
    {
        _command = command;
        _status = status;
        _output = output;
    }

    /// <summary>
    /// Updates the status of the command
    /// </summary>
    public void UpdateStatus(CommandStatus status)
    {
        _status = status;
    }

    /// <summary>
    /// Updates the output of the command
    /// </summary>
    public void UpdateOutput(string output)
    {
        _output = output;
    }

    public IRenderable Render()
    {
        var panel = new Panel(CreateContent())
        {
            Header = new PanelHeader($"[bold]Command:[/] {Markup.Escape(_command)}"),
            Border = BoxBorder.Rounded,
            BorderStyle = GetBorderStyle()
        };

        return panel;
    }

    private IRenderable CreateContent()
    {
        var content = new Rows(
            CreateStatusLine(),
            new Text(""),
            CreateOutputSection()
        );

        return content;
    }

    private IRenderable CreateStatusLine()
    {
        var statusText = _status switch
        {
            CommandStatus.Running => "[yellow]⏳ Running...[/]",
            CommandStatus.Success => "[green]✓ Success[/]",
            CommandStatus.Error => "[red]✗ Error[/]",
            CommandStatus.Interrupted => "[orange3]⚠ Interrupted[/]",
            _ => "[dim]Unknown[/]"
        };

        return new Markup($"Status: {statusText}");
    }

    private IRenderable CreateOutputSection()
    {
        if (string.IsNullOrWhiteSpace(_output))
        {
            return new Markup("[dim]No output[/]");
        }

        var outputPanel = new Panel(new Markup(Markup.Escape(_output)))
        {
            Header = new PanelHeader("[dim]Output[/]"),
            Border = BoxBorder.None,
            Padding = new Padding(1, 0)
        };

        return outputPanel;
    }

    private Style GetBorderStyle()
    {
        return _status switch
        {
            CommandStatus.Running => Style.Parse("yellow"),
            CommandStatus.Success => Style.Parse("green"),
            CommandStatus.Error => Style.Parse("red"),
            CommandStatus.Interrupted => Style.Parse("orange3"),
            _ => Style.Parse("dim")
        };
    }
}
