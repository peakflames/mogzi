namespace MaxBot.TUI;

public class DiffCard : ITuiCard
{
    private readonly string _filePath;
    private readonly string _diff;

    public DiffCard(string filePath, string diff)
    {
        _filePath = filePath;
        _diff = diff;
    }

    public IRenderable Render()
    {
        var panel = new Panel(CreateDiffView())
            .Header($"Diff: {_filePath}")
            .Border(BoxBorder.Rounded);
        return panel;
    }

    private IRenderable CreateDiffView()
    {
        var sb = new StringBuilder();
        foreach (var line in _diff.Split('\n'))
        {
            if (line.StartsWith("+"))
            {
                sb.AppendLine($"[green]{Markup.Escape(line)}[/]");
            }
            else if (line.StartsWith("-"))
            {
                sb.AppendLine($"[red]{Markup.Escape(line)}[/]");
            }
            else
            {
                sb.AppendLine(Markup.Escape(line));
            }
        }
        return new Markup(sb.ToString());
    }
}
