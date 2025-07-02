namespace Mogzi.TUI.Services;

public interface IUserSelectionProvider
{
    string Command { get; }
    string Description { get; }
    Task<List<CompletionItem>> GetSelectionsAsync();
    Task OnSelectionAsync(string selection);
}
