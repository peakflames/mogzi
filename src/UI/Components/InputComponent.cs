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
    
    // Enhanced input features for advanced keyboard handling
    private int _cursorPosition = 0;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    private bool _hasSelection = false;

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
        ResetCursorAndSelection();
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

    // Enhanced keyboard handling methods for advanced input features
    
    /// <summary>
    /// Inserts a character at the current cursor position.
    /// </summary>
    public void InsertCharacter(char character)
    {
        if (!_isInputEnabled) return;
        
        // Clear selection if any
        if (_hasSelection)
        {
            DeleteSelection();
        }
        
        // Ensure cursor position is valid
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        
        // Insert character at cursor position
        _currentInput = _currentInput.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        
        _commandHistoryIndex = -1; // Reset history navigation
    }
    
    /// <summary>
    /// Deletes the character before the cursor (backspace).
    /// </summary>
    public void DeleteCharacterBefore()
    {
        if (!_isInputEnabled || _currentInput.Length == 0) return;
        
        // If there's a selection, delete it
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        // Ensure cursor position is valid
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        
        if (_cursorPosition > 0)
        {
            _currentInput = _currentInput.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
        }
        
        _commandHistoryIndex = -1; // Reset history navigation
    }
    
    /// <summary>
    /// Deletes the character after the cursor (delete key).
    /// </summary>
    public void DeleteCharacterAfter()
    {
        if (!_isInputEnabled || _currentInput.Length == 0) return;
        
        // If there's a selection, delete it
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        // Ensure cursor position is valid
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        
        if (_cursorPosition < _currentInput.Length)
        {
            _currentInput = _currentInput.Remove(_cursorPosition, 1);
        }
        
        _commandHistoryIndex = -1; // Reset history navigation
    }
    
    /// <summary>
    /// Moves the cursor to the specified position.
    /// </summary>
    public void MoveCursor(int position)
    {
        _cursorPosition = Math.Max(0, Math.Min(position, _currentInput.Length));
        ClearSelection();
    }
    
    /// <summary>
    /// Moves the cursor left by one position.
    /// </summary>
    public void MoveCursorLeft(bool extendSelection = false)
    {
        if (_cursorPosition > 0)
        {
            if (extendSelection)
            {
                if (!_hasSelection)
                {
                    _selectionStart = _cursorPosition;
                    _hasSelection = true;
                }
                _cursorPosition--;
                _selectionEnd = _cursorPosition;
            }
            else
            {
                _cursorPosition--;
                ClearSelection();
            }
        }
    }
    
    /// <summary>
    /// Moves the cursor right by one position.
    /// </summary>
    public void MoveCursorRight(bool extendSelection = false)
    {
        if (_cursorPosition < _currentInput.Length)
        {
            if (extendSelection)
            {
                if (!_hasSelection)
                {
                    _selectionStart = _cursorPosition;
                    _hasSelection = true;
                }
                _cursorPosition++;
                _selectionEnd = _cursorPosition;
            }
            else
            {
                _cursorPosition++;
                ClearSelection();
            }
        }
    }
    
    /// <summary>
    /// Moves the cursor to the beginning of the input.
    /// </summary>
    public void MoveCursorToStart(bool extendSelection = false)
    {
        if (extendSelection)
        {
            if (!_hasSelection)
            {
                _selectionStart = _cursorPosition;
                _hasSelection = true;
            }
            _cursorPosition = 0;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = 0;
            ClearSelection();
        }
    }
    
    /// <summary>
    /// Moves the cursor to the end of the input.
    /// </summary>
    public void MoveCursorToEnd(bool extendSelection = false)
    {
        if (extendSelection)
        {
            if (!_hasSelection)
            {
                _selectionStart = _cursorPosition;
                _hasSelection = true;
            }
            _cursorPosition = _currentInput.Length;
            _selectionEnd = _cursorPosition;
        }
        else
        {
            _cursorPosition = _currentInput.Length;
            ClearSelection();
        }
    }
    
    /// <summary>
    /// Gets the current cursor position.
    /// </summary>
    public int GetCursorPosition()
    {
        return _cursorPosition;
    }
    
    /// <summary>
    /// Gets whether there is currently a text selection.
    /// </summary>
    public bool HasSelection()
    {
        return _hasSelection;
    }
    
    /// <summary>
    /// Gets the selected text, if any.
    /// </summary>
    public string GetSelectedText()
    {
        if (!_hasSelection) return string.Empty;
        
        var start = Math.Min(_selectionStart, _selectionEnd);
        var end = Math.Max(_selectionStart, _selectionEnd);
        var length = end - start;
        
        if (start >= 0 && start < _currentInput.Length && length > 0)
        {
            return _currentInput.Substring(start, Math.Min(length, _currentInput.Length - start));
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Clears the current text selection.
    /// </summary>
    public void ClearSelection()
    {
        _hasSelection = false;
        _selectionStart = -1;
        _selectionEnd = -1;
    }
    
    /// <summary>
    /// Deletes the currently selected text.
    /// </summary>
    private void DeleteSelection()
    {
        if (!_hasSelection) return;
        
        var start = Math.Min(_selectionStart, _selectionEnd);
        var end = Math.Max(_selectionStart, _selectionEnd);
        var length = end - start;
        
        if (start >= 0 && start < _currentInput.Length && length > 0)
        {
            _currentInput = _currentInput.Remove(start, Math.Min(length, _currentInput.Length - start));
            _cursorPosition = start;
        }
        
        ClearSelection();
    }
    
    /// <summary>
    /// Resets cursor position and selection when input changes externally.
    /// </summary>
    private void ResetCursorAndSelection()
    {
        _cursorPosition = Math.Min(_cursorPosition, _currentInput.Length);
        ClearSelection();
    }
}
