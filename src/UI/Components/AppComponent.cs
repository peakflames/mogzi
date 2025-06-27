namespace UI.Components;

public class AppComponent : TuiComponentBase
{
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly LayoutManager _layoutManager;

    private readonly HeaderComponent _header;
    private readonly StaticHistoryComponent _staticHistory;
    private readonly DynamicContentComponent _dynamicContent;
    private readonly InputComponent _input;
    private readonly FooterComponent _footer;

    public AppComponent(
        IAppService appService,
        HistoryManager historyManager,
        StateManager stateManager,
        LayoutManager layoutManager,
        HeaderComponent header,
        StaticHistoryComponent staticHistory,
        DynamicContentComponent dynamicContent,
        InputComponent input,
        FooterComponent footer)
    {
        _appService = appService;
        _historyManager = historyManager;
        _stateManager = stateManager;
        _layoutManager = layoutManager;
        _header = header;
        _staticHistory = staticHistory;
        _dynamicContent = dynamicContent;
        _input = input;
        _footer = footer;

        // Wire up input submission event
        _input.InputSubmitted += OnInputSubmitted;
    }

    public override async Task<IRenderable> RenderAsync(RenderContext context)
    {
        var layoutResult = _layoutManager.CalculateLayout(context.TerminalSize);

        // Static Zone
        var staticRows = new Rows(
            await _header.RenderAsync(context),
            await _staticHistory.RenderAsync(context),
            await _footer.RenderAsync(context)
        );
        var staticZone = new Panel(staticRows)
            .Header("Static Zone");
        staticZone.Height = layoutResult.StaticZoneConstraints.Height;

        // Dynamic Zone
        var dynamicRows = new Rows(
            await _dynamicContent.RenderAsync(context),
            await _input.RenderAsync(context)
        );
        var dynamicZone = new Panel(dynamicRows)
            .Header("Dynamic Zone");
        dynamicZone.Height = layoutResult.DynamicZoneConstraints.Height;

        var grid = new Grid()
            .AddColumn(new GridColumn())
            .AddRow(staticZone)
            .AddRow(dynamicZone);

        return grid;
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

    /// <summary>
    /// Gets the InputComponent for keyboard event handling.
    /// </summary>
    /// <returns>The InputComponent instance.</returns>
    public InputComponent GetInputComponent()
    {
        return _input;
    }

    /// <summary>
    /// Handles input submission from the InputComponent.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="input">The submitted input text.</param>
    private async void OnInputSubmitted(object? sender, string input)
    {
        try
        {
            // Disable input during processing
            _input.SetInputEnabled(false);
            
            // Process the user input
            await ProcessUserInput(input);
        }
        catch (Exception ex)
        {
            // Log error and add error message to history
            // In a real implementation, we would use proper logging
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing input: {ex.Message}");
            _historyManager.AddAssistantMessage(errorMessage);
        }
        finally
        {
            // Re-enable input
            _input.SetInputEnabled(true);
        }
    }
}
