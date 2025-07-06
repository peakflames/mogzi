namespace Mogzi.TUI.Components;

/// <summary>
/// Displays status information including directory, model, and token usage.
/// Handles dynamic status updates and supports configurable status items.
/// </summary>
public class FooterPanel : ITuiComponent
{
    public string Name => "FooterPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var currentDir = context.RenderingUtilities.FormatDisplayPath(
            context.TuiContext.WorkingDirectoryProvider.GetCurrentDirectory());
        var modelInfo = context.RenderingUtilities.FormatModelInfo(context.TuiContext.AppService);
        var tokenInfo = context.RenderingUtilities.FormatTokenUsage(
            context.TuiContext.AppService,
            context.TuiContext.HistoryManager.GetCurrentChatHistory());

        var content = $"[skyblue2]{currentDir}[/]  [rosybrown]{modelInfo}[/] [dim]({tokenInfo})[/]";

        return new Panel(new Markup(content))
            .NoBorder();
    }

    public Task<bool> HandleInputAsync(IRenderContext context, object inputEvent)
    {
        // Footer panel doesn't handle input events
        return Task.FromResult(false);
    }

    public Task InitializeAsync(IRenderContext context)
    {
        context.Logger.LogTrace("FooterPanel initialized");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
