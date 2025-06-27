namespace UI.Components;

public class AppComponent : TuiComponentBase
{
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly LayoutManager _layoutManager;

    public AppComponent(
        IAppService appService,
        HistoryManager historyManager,
        StateManager stateManager,
        LayoutManager layoutManager)
    {
        _appService = appService;
        _historyManager = historyManager;
        _stateManager = stateManager;
        _layoutManager = layoutManager;
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        // The layout manager will be responsible for rendering children.
        // For now, return a simple panel.
        return Task.FromResult<IRenderable>(new Panel("AppComponent"));
    }

    public async Task ProcessUserInput(string input)
    {
        var userMessage = new ChatMessage(ChatRole.User, input);
        _historyManager.AddUserMessage(userMessage);

        var chatHistory = _historyManager.GetCurrentChatHistory();
        var responseStream = _appService.ProcessChatMessageAsync(chatHistory, CancellationToken.None);

        await foreach (var responseUpdate in responseStream)
        {
            // In a real implementation, we would handle different kinds of updates.
            // For this test, we just need to get the final text.
            if (!string.IsNullOrEmpty(responseUpdate.Text))
            {
                var assistantMessage = new ChatMessage(ChatRole.Assistant, responseUpdate.Text);
                _historyManager.AddAssistantMessage(assistantMessage);
            }
        }
    }
}
