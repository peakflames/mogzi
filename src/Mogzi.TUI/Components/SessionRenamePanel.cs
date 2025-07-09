namespace Mogzi.TUI.Components;

/// <summary>
/// Displays a confirmation message when a session is renamed.
/// Shows a styled success message with the new session name.
/// </summary>
/// <remarks>
/// Initializes a new instance of SessionRenamePanel.
/// </remarks>
/// <param name="newSessionName">The new name of the session</param>
public class SessionRenamePanel(string newSessionName) : ITuiComponent
{
    private readonly string _newSessionName = newSessionName ?? throw new ArgumentNullException(nameof(newSessionName));

    public string Name => "SessionRenamePanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var panel = new Panel(new Markup($"[green]✓[/] Session renamed to '[cyan]{Markup.Escape(_newSessionName)}[/]'"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        return panel;
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Session rename panel doesn't handle input events
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
