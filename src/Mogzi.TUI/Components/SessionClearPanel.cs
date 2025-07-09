namespace Mogzi.TUI.Components;

/// <summary>
/// Displays a confirmation message when session history is cleared.
/// Shows a styled success message with checkmark.
/// </summary>
public class SessionClearPanel : ITuiComponent
{
    public string Name => "SessionClearPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var content = new Rows(
            new Markup("[green]✓ Session Cleared[/]"),
            new Markup("[dim]All messages have been removed from the current session[/]")
        );

        var panel = new Panel(content)
            .Header(" Session Management ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        return panel;
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Session clear panel doesn't handle input events
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
