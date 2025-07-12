namespace Mogzi.TUI.Components;

/// <summary>
/// Displays a confirmation message when chat history is cleared.
/// Shows a styled success message with checkmark.
/// </summary>
public class ClearPanel : ITuiComponent
{
    public string Name => "ClearPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var panel = new Panel(new Markup("[green]✓[/] Chat history cleared"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        return panel;
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Clear panel doesn't handle input events
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
