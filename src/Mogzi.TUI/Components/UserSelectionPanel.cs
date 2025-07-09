namespace Mogzi.TUI.Components;

/// <summary>
/// Displays interactive selection options for user commands.
/// Handles option navigation and selection for different selection providers.
/// </summary>
public class UserSelectionPanel : ITuiComponent
{
    public string Name => "UserSelectionPanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var inputContext = context.TuiContext.InputContext;

        if (inputContext.CompletionItems.Count == 0)
        {
            return new Text(string.Empty);
        }

        var selectionItems = inputContext.CompletionItems.Select((item, index) =>
        {
            var isSelected = index == inputContext.SelectedSuggestionIndex;
            var style = isSelected ? "[blue on white]" : "[dim]";
            var prefix = isSelected ? ">" : " ";

            return new Markup($"{style}{prefix} {item.Text,-12} {item.Description}[/]");
        }).ToArray();

        // Determine the appropriate header based on the active provider
        var header = GetContextualHeader(inputContext);

        return new Panel(new Rows(selectionItems))
            .Header(header)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(0, 0);
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

    /// <summary>
    /// Gets a contextual header based on the active provider.
    /// </summary>
    private static string GetContextualHeader(InputContext inputContext)
    {
        // Check if this is a session list provider
        if (inputContext.ActiveProvider?.GetType().Name == "SessionListProvider")
        {
            // Try to get the session limit from the provider
            var sessionCount = inputContext.CompletionItems.Count;
            return $"Available Chat Sessions (last {sessionCount} recently used)";
        }

        // Default header for other providers
        return "Select an option";
    }
}
