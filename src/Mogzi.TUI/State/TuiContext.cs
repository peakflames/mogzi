namespace Mogzi.TUI.State;

/// <summary>
/// Concrete implementation of ITuiContext that provides shared context and services for TUI states.
/// </summary>
public class TuiContext(
    InputContext inputContext,
    IServiceProvider serviceProvider,
    ILogger<TuiContext> logger,
    IScrollbackTerminal scrollbackTerminal,
    HistoryManager historyManager,
    SessionManager sessionManager,
    AutocompleteManager autocompleteManager,
    UserSelectionManager userSelectionManager,
    SlashCommandProcessor slashCommandProcessor,
    IWorkingDirectoryProvider workingDirectoryProvider,
    ToolResponseParser toolResponseParser,
    IAppService appService,
    IAiProcessingCoordinator aiProcessingCoordinator) : ITuiContext
{

    public InputContext InputContext { get; } = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
    public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    public ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));
    public IScrollbackTerminal ScrollbackTerminal { get; } = scrollbackTerminal ?? throw new ArgumentNullException(nameof(scrollbackTerminal));
    public HistoryManager HistoryManager { get; } = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
    public SessionManager SessionManager { get; } = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    public AutocompleteManager AutocompleteManager { get; } = autocompleteManager ?? throw new ArgumentNullException(nameof(autocompleteManager));
    public UserSelectionManager UserSelectionManager { get; } = userSelectionManager ?? throw new ArgumentNullException(nameof(userSelectionManager));
    public SlashCommandProcessor SlashCommandProcessor { get; } = slashCommandProcessor ?? throw new ArgumentNullException(nameof(slashCommandProcessor));
    public IWorkingDirectoryProvider WorkingDirectoryProvider { get; } = workingDirectoryProvider ?? throw new ArgumentNullException(nameof(workingDirectoryProvider));
    public ToolResponseParser ToolResponseParser { get; } = toolResponseParser ?? throw new ArgumentNullException(nameof(toolResponseParser));
    public IAppService AppService { get; } = appService ?? throw new ArgumentNullException(nameof(appService));
    public IAiProcessingCoordinator AiProcessCoordinator { get; } = aiProcessingCoordinator ?? throw new ArgumentNullException(nameof(aiProcessingCoordinator));

    public string ToolProgress { get; set; } = string.Empty;
    public string CurrentToolName { get; set; } = string.Empty;
    public List<string> CommandHistory { get; } = [];
    public int CommandHistoryIndex { get; set; } = -1;
    public DateTime AiOperationStartTime { get; set; } = DateTime.Now;
    public CancellationTokenSource? AiOperationCts { get; set; }
    public ConcurrentDictionary<string, string> FunctionCallToToolName { get; } = new();
    public ConcurrentDictionary<string, string> FunctionCallToPreEditContent { get; } = new();
    public bool AutoSubmitPipedInput { get; set; } = false;

    public async Task RequestStateTransitionAsync(ChatState newState)
    {
        var stateManager = ServiceProvider.GetRequiredService<ITuiStateManager>();
        await stateManager.TransitionToStateAsync(newState);
    }

    public void NotifyStateChanged()
    {
        // This could be used to trigger UI updates if needed
        // For now, the dynamic rendering handles state changes automatically
    }
}
