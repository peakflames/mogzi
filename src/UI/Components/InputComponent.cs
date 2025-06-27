namespace UI.Components;

public class InputComponent : TuiComponentBase
{
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<InputComponent>? _logger;
    
    // Input state
    private string _currentInput = string.Empty;
    private int _commandHistoryIndex = -1;
    private bool _isInputEnabled = true;
    private readonly List<string> _commandHistory = new();

    public InputComponent(
        HistoryManager historyManager,
        StateManager stateManager,
        ILogger<InputComponent>? logger = null)
    {
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger;
        
        // Initialize command history from HistoryManager
        LoadCommandHistory();
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        try
        {
            var content = BuildInputContent(context);
            var title = BuildInputTitle(context);
            
            var panel = new Panel(content)
                .Header(title)
                .Border(BoxBorder.Rounded)
                .BorderColor(GetInputBorderColor());
                
            return Task.FromResult<IRenderable>(panel);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering InputComponent");
            
            // Fallback rendering in case of errors
            var errorPanel = new Panel(new Markup("[red]Error loading input[/]"))
                .Header("Input - Error")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);
                
            return Task.FromResult<IRenderable>(errorPanel);
        }
    }

    private string BuildInputTitle(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        
        if (!_isInputEnabled)
        {
            return "Input - Processing...";
        }
        
        // Base title
        var title = "Input";
        
        // Add command history info if there's space
        if (terminalWidth >= 100 && _commandHistory.Count > 0)
        {
            title += $" - {_commandHistory.Count} commands in history";
        }
        
        // Add help hint if there's space
        if (terminalWidth >= 120)
        {
            title += " - ↑/↓ for history, Enter to send";
        }
        
        return title;
    }

    private IRenderable BuildInputContent(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        
        if (!_isInputEnabled)
        {
            return BuildDisabledInputContent();
        }
        
        if (terminalWidth >= 120)
        {
            // Wide terminal - show full input interface
            return BuildWideInputContent();
        }
        else if (terminalWidth >= 90)
        {
            // Medium terminal - show essential input interface
            return BuildMediumInputContent();
        }
        else
        {
            // Narrow terminal - show minimal input interface
            return BuildNarrowInputContent();
        }
    }

    private IRenderable BuildWideInputContent()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Prompt").Width(8))
            .AddColumn(new TableColumn("Input").Width(60))
            .AddColumn(new TableColumn("Status").Width(20))
            .AddColumn(new TableColumn("Help").Width(25));

        var inputDisplay = string.IsNullOrEmpty(_currentInput) ? "[dim]Type your message...[/]" : _currentInput;
        var statusInfo = GetInputStatusInfo();
        var helpInfo = GetHelpInfo();

        table.AddRow(
            "[blue]>[/]",
            $"[white]{inputDisplay}[/]",
            $"[green]{statusInfo}[/]",
            $"[yellow]{helpInfo}[/]"
        );

        return table;
    }

    private IRenderable BuildMediumInputContent()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Prompt").Width(8))
            .AddColumn(new TableColumn("Input").Width(50))
            .AddColumn(new TableColumn("Status").Width(20));

        var inputDisplay = string.IsNullOrEmpty(_currentInput) ? "[dim]Type your message...[/]" : _currentInput;
        var statusInfo = GetInputStatusInfo();

        table.AddRow(
            "[blue]>[/]",
            $"[white]{inputDisplay}[/]",
            $"[green]{statusInfo}[/]"
        );

        return table;
    }

    private IRenderable BuildNarrowInputContent()
    {
        var inputDisplay = string.IsNullOrEmpty(_currentInput) ? "[dim]Type your message...[/]" : _currentInput;
        return new Markup($"[blue]>[/] [white]{inputDisplay}[/]");
    }

    private IRenderable BuildDisabledInputContent()
    {
        return new Markup("[dim]Input disabled - AI is processing...[/]");
    }

    private Color GetInputBorderColor()
    {
        if (!_isInputEnabled)
        {
            return Color.Yellow; // Processing state
        }
        
        // Check if there are pending state changes (indicates activity)
        if (_stateManager.HasPendingChanges)
        {
            return Color.Yellow; // Active state
        }
        
        return Color.Green; // Ready state
    }

    private string GetInputStatusInfo()
    {
        if (!_isInputEnabled)
        {
            return "Processing";
        }
        
        var pendingMessages = _historyManager.GetPendingMessages();
        if (pendingMessages.Any())
        {
            return "Active";
        }
        
        return "Ready";
    }

    private string GetHelpInfo()
    {
        if (_commandHistory.Count > 0)
        {
            return "↑/↓ History, Enter Send";
        }
        
        return "Enter to Send";
    }

    private void LoadCommandHistory()
    {
        try
        {
            // Load command history from completed user messages
            var userMessages = _historyManager.GetCompletedMessages()
                .Where(m => m.Role == ChatRole.User)
                .Select(m => m.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
            
            _commandHistory.Clear();
            _commandHistory.AddRange(userMessages);
            
            _logger?.LogDebug("Loaded {Count} commands from history", _commandHistory.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading command history");
        }
    }

    // Public methods for input handling (would be called by keyboard event handlers)
    public void SetCurrentInput(string input)
    {
        _currentInput = input ?? string.Empty;
        _commandHistoryIndex = -1; // Reset history navigation
    }

    public void SetInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;
    }

    public string GetCurrentInput()
    {
        return _currentInput;
    }

    public void NavigateCommandHistory(bool up)
    {
        if (_commandHistory.Count == 0) return;
        
        if (up)
        {
            // Navigate up (older commands) - start from most recent and go backwards
            if (_commandHistoryIndex == -1)
            {
                // First time navigating up - go to most recent command (last in list)
                _commandHistoryIndex = _commandHistory.Count - 1;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex > 0)
            {
                // Go to older command (earlier in list)
                _commandHistoryIndex--;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
            // If already at oldest (index 0), stay there
        }
        else
        {
            // Navigate down (newer commands)
            if (_commandHistoryIndex >= 0 && _commandHistoryIndex < _commandHistory.Count - 1)
            {
                // Go to newer command (later in list)
                _commandHistoryIndex++;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex == _commandHistory.Count - 1)
            {
                // Go back to empty input (beyond newest)
                _commandHistoryIndex = -1;
                _currentInput = string.Empty;
            }
            // If already at newest (empty), stay there
        }
    }

    public void AddToCommandHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        
        // Check if command already exists
        if (_commandHistory.Contains(command))
        {
            // Don't add duplicates - just ignore
            return;
        }
        
        // Add the command to the end (most recent)
        _commandHistory.Add(command);
        
        // Limit history size (keep last 100 commands)
        if (_commandHistory.Count > 100)
        {
            _commandHistory.RemoveAt(0);
        }
        
        _commandHistoryIndex = -1; // Reset navigation
    }

    public void ClearCurrentInput()
    {
        _currentInput = string.Empty;
        _commandHistoryIndex = -1;
    }

    // Event for when input is submitted (would be triggered by Enter key)
    public event EventHandler<string>? InputSubmitted;

    protected virtual void OnInputSubmitted(string input)
    {
        InputSubmitted?.Invoke(this, input);
    }

    public void SubmitCurrentInput()
    {
        if (string.IsNullOrWhiteSpace(_currentInput) || !_isInputEnabled) return;
        
        var inputToSubmit = _currentInput;
        AddToCommandHistory(inputToSubmit);
        ClearCurrentInput();
        
        OnInputSubmitted(inputToSubmit);
    }
}
