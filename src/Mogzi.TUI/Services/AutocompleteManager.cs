namespace Mogzi.TUI.Services;

public class AutocompleteManager(IEnumerable<IAutocompleteProvider> providers, ILogger<AutocompleteManager> logger)
{
    private readonly List<IAutocompleteProvider> _providers = [.. providers];
    private readonly ILogger<AutocompleteManager> _logger = logger;

    public IAutocompleteProvider? DetectTrigger(string input, int cursorPosition)
    {
        // _logger.LogTrace("AutocompleteManager.DetectTrigger called with input='{Input}', cursorPosition={CursorPosition}", input, cursorPosition);

        try
        {
            foreach (var provider in _providers)
            {
                // _logger.LogTrace("Checking provider {ProviderType} (trigger: '{TriggerChar}')", provider.Type, provider.TriggerCharacter);
                if (provider.ShouldTrigger(input, cursorPosition))
                {
                    // _logger.LogTrace("Autocomplete triggered for provider {ProviderType}", provider.Type);
                    return provider;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting autocomplete trigger");
        }

        // _logger.LogTrace("No autocomplete provider triggered");
        return null;
    }

    public async Task UpdateSuggestionsAsync(InputContext context)
    {
        try
        {
            if (context.ActiveProvider is null)
            {
                context.ClearAutocomplete();
                return;
            }

            var partialInput = context.ActiveProvider.ExtractPartial(context.CurrentInput, context.CursorPosition);
            var suggestions = await context.ActiveProvider.GetSuggestionsAsync(partialInput);

            if (suggestions.Count > 0)
            {
                context.State = InputState.Autocomplete;
                context.ActiveAutocompleteType = context.ActiveProvider.Type;
                context.Suggestions = [.. suggestions.Select(s => s.Text)];
                context.CompletionItems = suggestions;
                context.ShowSuggestions = true;
                context.SelectedSuggestionIndex = 0;
            }
            else
            {
                context.ClearAutocomplete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating autocomplete suggestions");
            context.ClearAutocomplete();
        }
    }

    public void AcceptSuggestion(InputContext context)
    {
        try
        {
            if (context.ActiveProvider is null || !context.ShowSuggestions || context.Suggestions.Count == 0)
            {
                return;
            }

            var selectedCompletion = context.Suggestions[context.SelectedSuggestionIndex];
            var (newInput, newCursorPos) = context.ActiveProvider.ReplacePartial(
                context.CurrentInput,
                context.CursorPosition,
                selectedCompletion);

            context.CurrentInput = newInput;
            context.CursorPosition = newCursorPos;
            context.ClearAutocomplete();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting autocomplete suggestion");
            context.ClearAutocomplete();
        }
    }
}
