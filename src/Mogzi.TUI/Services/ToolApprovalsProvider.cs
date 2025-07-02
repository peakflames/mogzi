namespace Mogzi.TUI.Services;

public class ToolApprovalsProvider(ChatClient chatClient) : IUserSelectionProvider
{
    private readonly ChatClient _chatClient = chatClient;

    public string Command => "/tool-approvals";
    public string Description => "Set the tool approval mode for the current session.";

    public Task<List<CompletionItem>> GetSelectionsAsync()
    {
        var selections = new List<CompletionItem>
        {
            new("readonly", "Approve only read-only tool operations."),
            new("all", "Approve all tool operations, including file writes and commands.")
        };
        return Task.FromResult(selections);
    }

    public Task OnSelectionAsync(string selection)
    {
        if (selection.Equals("readonly", StringComparison.OrdinalIgnoreCase) ||
            selection.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            _chatClient.Config.ToolApprovals = selection.ToLowerInvariant();
        }
        return Task.CompletedTask;
    }
}
