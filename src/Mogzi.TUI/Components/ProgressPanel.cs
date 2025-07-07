namespace Mogzi.TUI.Components;

/// <summary>
/// Displays animated progress indicators for AI operations.
/// Shows operation status, duration, and supports different progress types.
/// </summary>
public class ProgressPanel : ITuiComponent
{
    public string Name => "ProgressPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var currentState = context.CurrentState;

        return currentState switch
        {
            ChatState.Thinking => RenderThinkingProgress(context),
            ChatState.ToolExecution => RenderToolExecutionProgress(context),
            _ => new Text(string.Empty)
        };
    }

    private IRenderable RenderThinkingProgress(IRenderContext context)
    {
        var duration = DateTime.Now - context.TuiContext.AiOperationStartTime;
        var durationText = $"{duration.TotalSeconds:F1}s";

        // Create animated thinking indicator
        var animationFrame = GetAnimationFrame();
        var content = $"[yellow]{animationFrame}[/] [dim]Thinking... ({durationText}) [grey]Press Esc to cancel[/][/]";

        return new Panel(new Markup(content))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0, 1, 0);
    }

    private IRenderable RenderToolExecutionProgress(IRenderContext context)
    {
        var toolName = context.TuiContext.CurrentToolName;
        var progress = context.TuiContext.ToolProgress;

        if (string.IsNullOrEmpty(toolName) && string.IsNullOrEmpty(progress))
        {
            return new Text(string.Empty);
        }

        var animationFrame = GetAnimationFrame();
        var displayText = !string.IsNullOrEmpty(toolName) ? toolName : progress;
        var content = $"[green]{animationFrame}[/] [dim]{displayText}[/]";

        return new Panel(new Markup(content))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0, 1, 0);
    }

    private string GetAnimationFrame()
    {
        // Simple rotating animation
        var frames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var frameIndex = DateTime.Now.Millisecond / 100 % frames.Length;
        return frames[frameIndex];
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Progress panel doesn't handle input events directly
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
