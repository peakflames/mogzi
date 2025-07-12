namespace Mogzi.TUI.Components;

/// <summary>
/// Displays autocomplete suggestions with selection highlighting.
/// Handles navigation and selection logic for different completion types.
/// </summary>
public class AutocompletePanel : ITuiComponent
{
    public string Name => "AutocompletePanel";
    public bool IsVisible { get; set; } = true;

    public IRenderable Render(IRenderContext context)
    {
        var inputContext = context.TuiContext.InputContext;

        if (!inputContext.ShowSuggestions || inputContext.CompletionItems.Count == 0)
        {
            return new Text(string.Empty);
        }

        var maxSuggestionLength = inputContext.CompletionItems.Any()
            ? inputContext.CompletionItems.Max(s => s.Text.Length)
            : 0;

        var suggestionItems = inputContext.CompletionItems.Select((suggestion, index) =>
        {
            var isSelected = index == inputContext.SelectedSuggestionIndex;
            var style = isSelected ? "[blue on white]" : "[dim]";
            var prefix = isSelected ? ">" : " ";

            var displayText = suggestion.Text.PadRight(maxSuggestionLength);
            var description = suggestion.Description;

            return new Markup($"{style}{prefix} {displayText}  {description}[/]");
        }).ToArray();

        return new Panel(new Rows(suggestionItems))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
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
}
