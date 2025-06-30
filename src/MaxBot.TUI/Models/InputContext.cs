namespace MaxBot.TUI.Models;

/// <summary>
/// Represents the state of the input system, including autocomplete functionality.
/// </summary>
public class InputContext
{
    /// <summary>
    /// The current input text.
    /// </summary>
    public string CurrentInput { get; set; } = string.Empty;

    /// <summary>
    /// The current cursor position within the input text.
    /// </summary>
    public int CursorPosition { get; set; } = 0;

    /// <summary>
    /// The current state of the input system.
    /// </summary>
    public InputState State { get; set; } = InputState.Normal;

    /// <summary>
    /// The type of autocomplete currently active.
    /// </summary>
    public AutocompleteType ActiveAutocompleteType { get; set; } = AutocompleteType.None;

    /// <summary>
    /// The currently active autocomplete provider.
    /// </summary>
    public IAutocompleteProvider? ActiveProvider { get; set; }

    /// <summary>
    /// List of autocomplete suggestions (text only for backward compatibility).
    /// </summary>
    public List<string> Suggestions { get; set; } = [];

    /// <summary>
    /// List of full completion items with metadata.
    /// </summary>
    public List<CompletionItem> CompletionItems { get; set; } = [];

    /// <summary>
    /// The index of the currently selected suggestion.
    /// </summary>
    public int SelectedSuggestionIndex { get; set; } = 0;

    /// <summary>
    /// Whether to show the autocomplete suggestions.
    /// </summary>
    public bool ShowSuggestions { get; set; } = false;

    /// <summary>
    /// Clears the autocomplete state.
    /// </summary>
    public void ClearAutocomplete()
    {
        State = InputState.Normal;
        ActiveAutocompleteType = AutocompleteType.None;
        ActiveProvider = null;
        ShowSuggestions = false;
        Suggestions.Clear();
        CompletionItems.Clear();
        SelectedSuggestionIndex = 0;
    }

    /// <summary>
    /// Clears all input state.
    /// </summary>
    public void Clear()
    {
        CurrentInput = string.Empty;
        CursorPosition = 0;
        ClearAutocomplete();
    }
}

/// <summary>
/// Represents the different states of the input system.
/// </summary>
public enum InputState
{
    /// <summary>
    /// Normal input mode.
    /// </summary>
    Normal,

    /// <summary>
    /// Showing autocomplete suggestions.
    /// </summary>
    Autocomplete
}
