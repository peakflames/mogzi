namespace Mogzi.TUI.State;

/// <summary>
/// Concrete implementation of ITuiContext that provides shared context and services for TUI states.
/// </summary>
public class TuiContext : ITuiContext
{
    private readonly ITuiStateManager _stateManager;

    public TuiContext(
        InputContext inputContext,
        IServiceProvider serviceProvider,
        ILogger<TuiContext> logger,
        IScrollbackTerminal scrollbackTerminal,
        HistoryManager historyManager,
        AutocompleteManager autocompleteManager,
        UserSelectionManager userSelectionManager,
        SlashCommandProcessor slashCommandProcessor,
        IWorkingDirectoryProvider workingDirectoryProvider,
        ToolResponseParser toolResponseParser,
        IAppService appService,
        ITuiStateManager stateManager)
    {
        InputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ScrollbackTerminal = scrollbackTerminal ?? throw new ArgumentNullException(nameof(scrollbackTerminal));
        HistoryManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        AutocompleteManager = autocompleteManager ?? throw new ArgumentNullException(nameof(autocompleteManager));
        UserSelectionManager = userSelectionManager ?? throw new ArgumentNullException(nameof(userSelectionManager));
        SlashCommandProcessor = slashCommandProcessor ?? throw new ArgumentNullException(nameof(slashCommandProcessor));
        WorkingDirectoryProvider = workingDirectoryProvider ?? throw new ArgumentNullException(nameof(workingDirectoryProvider));
        ToolResponseParser = toolResponseParser ?? throw new ArgumentNullException(nameof(toolResponseParser));
        AppService = appService ?? throw new ArgumentNullException(nameof(appService));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));

        CommandHistory = [];
        CommandHistoryIndex = -1;
        ToolProgress = string.Empty;
        CurrentToolName = string.Empty;
        AiOperationStartTime = DateTime.Now;
        FunctionCallToToolName = [];
        FunctionCallToPreEditContent = [];
    }

    public InputContext InputContext { get; }
    public IServiceProvider ServiceProvider { get; }
    public ILogger Logger { get; }
    public IScrollbackTerminal ScrollbackTerminal { get; }
    public HistoryManager HistoryManager { get; }
    public AutocompleteManager AutocompleteManager { get; }
    public UserSelectionManager UserSelectionManager { get; }
    public SlashCommandProcessor SlashCommandProcessor { get; }
    public IWorkingDirectoryProvider WorkingDirectoryProvider { get; }
    public ToolResponseParser ToolResponseParser { get; }
    public IAppService AppService { get; }

    public string ToolProgress { get; set; }
    public string CurrentToolName { get; set; }
    public List<string> CommandHistory { get; }
    public int CommandHistoryIndex { get; set; }
    public DateTime AiOperationStartTime { get; set; }
    public CancellationTokenSource? AiOperationCts { get; set; }
    public Dictionary<string, string> FunctionCallToToolName { get; }
    public Dictionary<string, string> FunctionCallToPreEditContent { get; }

    public async Task RequestStateTransitionAsync(ChatState newState)
    {
        await _stateManager.TransitionToStateAsync(newState);
    }

    public void NotifyStateChanged()
    {
        // This could be used to trigger UI updates if needed
        // For now, the dynamic rendering handles state changes automatically
    }
}
