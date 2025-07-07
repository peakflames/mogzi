namespace Mogzi.TUI.Components;

/// <summary>
/// Handles the main input area of the TUI application.
/// Manages cursor positioning, text display, and input validation.
/// </summary>
public class InputPanel : ITuiComponent
{
    public string Name => "InputPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var currentInput = context.TuiContext.InputContext.CurrentInput;

        string content;
        if (string.IsNullOrEmpty(currentInput))
        {
            content = $"{prompt}{cursor}[dim]Type your message or /help[/]";
        }
        else
        {
            // Insert cursor at the correct position
            var beforeCursor = currentInput[..context.TuiContext.InputContext.CursorPosition];
            var afterCursor = currentInput[context.TuiContext.InputContext.CursorPosition..];
            content = $"{prompt}{beforeCursor}{cursor}{afterCursor}";
        }

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey23)
            .Padding(1, 0, 1, 0)
            .Expand();
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Input handling is delegated to the state manager and mediator
        // This component focuses on rendering
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
