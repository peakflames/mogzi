namespace Mogzi.TUI.Components;

/// <summary>
/// Displays a goodbye message when the application is exiting.
/// Shows a styled farewell message with yellow styling.
/// </summary>
public class ExitPanel : ITuiComponent
{
    public string Name => "ExitPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var panel = new Panel(new Markup("[yellow]Goodbye![/] Exiting Mogzi..."))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);

        return panel;
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Exit panel doesn't handle input events
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
