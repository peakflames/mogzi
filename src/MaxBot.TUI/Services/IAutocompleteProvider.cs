namespace MaxBot.TUI.Services;

public interface IAutocompleteProvider
{
    AutocompleteType Type { get; }
    char TriggerCharacter { get; }

    bool ShouldTrigger(string input, int cursorPosition);
    string ExtractPartial(string input, int cursorPosition);
    Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput);
    (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion);
}
