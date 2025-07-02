namespace Mogzi.TUI.Services;

public class SlashCommandProvider(SlashCommandProcessor slashCommandProcessor, ILogger<SlashCommandProvider> logger) : IAutocompleteProvider
{
    private readonly SlashCommandProcessor _slashCommandProcessor = slashCommandProcessor;
    private readonly ILogger<SlashCommandProvider> _logger = logger;

    public AutocompleteType Type => AutocompleteType.SlashCommand;
    public char TriggerCharacter => '/';

    public bool ShouldTrigger(string input, int cursorPosition)
    {
        return InputUtils.ShouldShowSlashAutocomplete(input, cursorPosition);
    }

    public string ExtractPartial(string input, int cursorPosition)
    {
        return InputUtils.ExtractSlashCommand(input, cursorPosition);
    }

    public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
    {
        try
        {
            var suggestions = _slashCommandProcessor.GetCommandSuggestions(partialInput);
            var allCommands = _slashCommandProcessor.GetAllCommands();

            var completionItems = suggestions.Select(command =>
            {
                var description = allCommands.GetValueOrDefault(command, "");
                return new CompletionItem(command, description, CompletionItemType.Command);
            }).ToList();

            return await Task.FromResult(completionItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slash command suggestions for partial: {Partial}", partialInput);
            return [];
        }
    }

    public (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion)
    {
        return InputUtils.ReplaceSlashCommand(input, cursorPos, completion);
    }
}
