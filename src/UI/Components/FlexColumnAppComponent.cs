namespace UI.Components;

/// <summary>
/// Main application component implementing the Gemini-CLI flex column layout pattern.
/// This replaces the complex multi-zone layout with a simple single-column flow.
/// </summary>
public class FlexColumnAppComponent : TuiComponentBase
{
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<FlexColumnAppComponent>? _logger;

    // Input state
    private string _currentInput = string.Empty;
    private int _commandHistoryIndex = -1;
    private bool _isInputEnabled = true;
    private readonly List<string> _commandHistory = new();
    
    // Chat state
    private ChatState _currentState = ChatState.Input;
    private string _toolProgress = string.Empty;

    // Enhanced input features
    private int _cursorPosition = 0;
    private int _selectionStart = -1;
    private int _selectionEnd = -1;
    private bool _hasSelection = false;

    public FlexColumnAppComponent(
        IAppService appService,
        HistoryManager historyManager,
        StateManager stateManager,
        ILogger<FlexColumnAppComponent>? logger = null)
    {
        _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger;
        
        LoadCommandHistory();
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        try
        {
            var chatHistory = _historyManager.GetCurrentChatHistory();
            var layout = CreateFlexColumnLayout(chatHistory, context);
            return Task.FromResult<IRenderable>(layout);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering FlexColumnAppComponent");
            
            // Fallback rendering
            var errorPanel = new Panel(new Markup("[red]Error loading chat interface[/]"))
                .Header("MaxBot - Error")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);
                
            return Task.FromResult<IRenderable>(errorPanel);
        }
    }

    /// <summary>
    /// Creates the flex column layout - everything flows in a single column like Gemini-CLI.
    /// </summary>
    private IRenderable CreateFlexColumnLayout(List<ChatMessage> chatHistory, RenderContext context)
    {
        var contentItems = new List<IRenderable>();

        // Add header/welcome if chat is empty
        if (!chatHistory.Any())
        {
            AddWelcomeMessages(contentItems);
        }

        MessageType? prevMessageType = null;

        // Add all chat history with proper spacing
        foreach (var message in chatHistory)
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                // Add extra spacing when message types change
                if (prevMessageType != null && prevMessageType != GetMessageType(message))
                {
                    contentItems.Add(new Text(""));
                }

                var prefix = GetMessageType(message) switch
                {
                    MessageType.User => "[dim]>[/] ",
                    MessageType.Assistant => "✦ ",
                    MessageType.System => "",
                    _ => ""
                };

                var color = GetMessageType(message) switch
                {
                    MessageType.User => "dim",
                    MessageType.Assistant => "white",
                    MessageType.System => "grey69",
                    _ => "white"
                };

                contentItems.Add(new Markup($"[{color}]{prefix}{message.Text}[/]"));

                // Add extra spacing after user messages and AI responses
                if (GetMessageType(message) == MessageType.User || GetMessageType(message) == MessageType.Assistant)
                {
                    contentItems.Add(new Text(""));
                }

                prevMessageType = GetMessageType(message);
            }
        }

        // Add extra spacing before the bottom component
        contentItems.Add(new Text(""));

        // Add current state component at the bottom (like the last item in flex-col)
        var bottomComponent = _currentState switch
        {
            ChatState.Input => CreateInputComponent(),
            ChatState.Thinking => CreateThinkingComponent(),
            ChatState.ToolExecution => CreateToolExecutionComponent(),
            _ => CreateInputComponent()
        };

        contentItems.Add(bottomComponent);

        // Add extra spacing before footer
        contentItems.Add(new Text(""));

        // Add footer
        contentItems.Add(CreateFooterComponent());

        // Create the main content panel with all items flowing vertically
        var contentPanel = new Panel(new Rows(contentItems))
            .Padding(1, 0)
            .NoBorder();

        return contentPanel;
    }

    /// <summary>
    /// Adds welcome messages to the content when chat is empty.
    /// </summary>
    private void AddWelcomeMessages(List<IRenderable> contentItems)
    {
        contentItems.Add(new Markup("[bold blue]Max[/]"));
        contentItems.Add(new Text(""));
        contentItems.Add(new Markup("[grey69]Tips for getting started:[/]"));
        contentItems.Add(new Markup("[grey69]1. Ask questions, edit files, or run commands.[/]"));
        contentItems.Add(new Markup("[grey69]2. Be specific for the best results.[/]"));
        contentItems.Add(new Markup("[grey69]3. Create an .max/AGENT.md files to customize your interactions with Max.[/]"));
        contentItems.Add(new Markup("[grey69]4. /help for more information.[/]"));
        contentItems.Add(new Text(""));
    }

    /// <summary>
    /// Creates the input component for flex layout.
    /// </summary>
    private IRenderable CreateInputComponent()
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var placeholder = string.IsNullOrEmpty(_currentInput) ? "[dim]Type your message or @path/to/file[/]" : "";
        var content = $"{prompt}{_currentInput}{cursor}";

        return new Panel(new Markup(content))
            .NoBorder()
            .Expand();
    }

    /// <summary>
    /// Creates the thinking/processing component for flex layout.
    /// </summary>
    private IRenderable CreateThinkingComponent()
    {
        // Create animated leading dots
        var animationFrame = DateTime.Now.Millisecond / 250 % 4; // 4 frames, changes every 250ms
        var leadingAnimation = animationFrame switch
        {
            0 => "   ",
            1 => ".  ",
            2 => ".. ",
            3 => "...",
            _ => "   "
        };

        return new Panel(new Markup($"[orange3]{leadingAnimation}[/] [dim]AI is thinking...[/]"))
            .NoBorder();
    }

    /// <summary>
    /// Creates the tool execution component for flex layout.
    /// </summary>
    private IRenderable CreateToolExecutionComponent()
    {
        // Create animated leading dots
        var animationFrame = DateTime.Now.Millisecond / 250 % 4; // 4 frames, changes every 250ms
        var leadingAnimation = animationFrame switch
        {
            0 => "   ",
            1 => ".  ",
            2 => ".. ",
            3 => "...",
            _ => "   "
        };
        
        return new Markup($"[yellow]{leadingAnimation}[/] {_toolProgress}");
    }

    /// <summary>
    /// Creates the footer component.
    /// </summary>
    private IRenderable CreateFooterComponent()
    {
        var content = "[dim]~/Desktop/maxbot    no sandbox (see /docs)    gemini-2.5-pro (100% context left)[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }

    /// <summary>
    /// Converts ChatMessage to MessageType for consistent handling.
    /// </summary>
    private MessageType GetMessageType(ChatMessage message)
    {
        if (message.Role == ChatRole.User)
            return MessageType.User;
        else if (message.Role == ChatRole.Assistant)
            return MessageType.Assistant;
        else if (message.Role == ChatRole.System)
            return MessageType.System;
        else
            return MessageType.System;
    }

    /// <summary>
    /// Loads command history from completed user messages.
    /// </summary>
    private void LoadCommandHistory()
    {
        try
        {
            var chatHistory = _historyManager.GetCurrentChatHistory();
            var userMessages = chatHistory
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

    // Public methods for input handling (called by keyboard event handlers)
    
    public void SetCurrentInput(string input)
    {
        _currentInput = input ?? string.Empty;
        _commandHistoryIndex = -1;
        ResetCursorAndSelection();
    }

    public void SetInputEnabled(bool enabled)
    {
        _isInputEnabled = enabled;
        _currentState = enabled ? ChatState.Input : ChatState.Thinking;
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
            if (_commandHistoryIndex == -1)
            {
                _commandHistoryIndex = _commandHistory.Count - 1;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex > 0)
            {
                _commandHistoryIndex--;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
        }
        else
        {
            if (_commandHistoryIndex >= 0 && _commandHistoryIndex < _commandHistory.Count - 1)
            {
                _commandHistoryIndex++;
                _currentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex == _commandHistory.Count - 1)
            {
                _commandHistoryIndex = -1;
                _currentInput = string.Empty;
            }
        }
    }

    public void AddToCommandHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return;
        
        if (_commandHistory.Contains(command))
        {
            return;
        }
        
        _commandHistory.Add(command);
        
        if (_commandHistory.Count > 100)
        {
            _commandHistory.RemoveAt(0);
        }
        
        _commandHistoryIndex = -1;
    }

    public void ClearCurrentInput()
    {
        _currentInput = string.Empty;
        _commandHistoryIndex = -1;
    }

    public async Task SubmitCurrentInput()
    {
        if (string.IsNullOrWhiteSpace(_currentInput) || !_isInputEnabled) return;
        
        var inputToSubmit = _currentInput;
        AddToCommandHistory(inputToSubmit);
        ClearCurrentInput();
        
        // Process the input
        await ProcessUserInput(inputToSubmit);
    }

    /// <summary>
    /// Determines if a ChatResponseUpdate represents tool execution.
    /// </summary>
    private bool IsToolExecutionUpdate(ChatResponseUpdate responseUpdate)
    {
        if (responseUpdate.Contents == null) return false;
        
        return responseUpdate.Contents.Any(content => 
            content is FunctionCallContent || content is FunctionResultContent);
    }

    /// <summary>
    /// Processes user input through the app service.
    /// </summary>
    private async Task ProcessUserInput(string input)
    {
        try
        {
            // Disable input during processing
            SetInputEnabled(false);
            
            // Add user message to history
            var userMessage = new ChatMessage(ChatRole.User, input);
            _historyManager.AddUserMessage(userMessage);

            var chatHistory = _historyManager.GetCurrentChatHistory();
            var responseStream = _appService.ProcessChatMessageAsync(chatHistory, CancellationToken.None);

            await foreach (var responseUpdate in responseStream)
            {
                // Handle different types of updates
                if (!string.IsNullOrEmpty(responseUpdate.Text))
                {
                    var assistantMessage = new ChatMessage(ChatRole.Assistant, responseUpdate.Text);
                    _historyManager.AddAssistantMessage(assistantMessage);
                }
                
                // Handle tool execution updates
                if (IsToolExecutionUpdate(responseUpdate))
                {
                    _currentState = ChatState.ToolExecution;
                    _toolProgress = responseUpdate.Text ?? "Executing tool...";
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing user input");
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing input: {ex.Message}");
            _historyManager.AddAssistantMessage(errorMessage);
        }
        finally
        {
            // Re-enable input
            SetInputEnabled(true);
        }
    }

    // Enhanced keyboard handling methods
    
    public void InsertCharacter(char character)
    {
        if (!_isInputEnabled) return;
        
        if (_hasSelection)
        {
            DeleteSelection();
        }
        
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        _currentInput = _currentInput.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        _commandHistoryIndex = -1;
    }
    
    public void DeleteCharacterBefore()
    {
        if (!_isInputEnabled || _currentInput.Length == 0) return;
        
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        
        if (_cursorPosition > 0)
        {
            _currentInput = _currentInput.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
        }
        
        _commandHistoryIndex = -1;
    }
    
    public void DeleteCharacterAfter()
    {
        if (!_isInputEnabled || _currentInput.Length == 0) return;
        
        if (_hasSelection)
        {
            DeleteSelection();
            return;
        }
        
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        
        if (_cursorPosition < _currentInput.Length)
        {
            _currentInput = _currentInput.Remove(_cursorPosition, 1);
        }
        
        _commandHistoryIndex = -1;
    }
    
    public void MoveCursor(int position)
    {
        _cursorPosition = Math.Max(0, Math.Min(position, _currentInput.Length));
        ClearSelection();
    }
    
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
    
    public int GetCursorPosition()
    {
        return _cursorPosition;
    }
    
    public bool HasSelection()
    {
        return _hasSelection;
    }
    
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
    
    public void ClearSelection()
    {
        _hasSelection = false;
        _selectionStart = -1;
        _selectionEnd = -1;
    }
    
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
    
    private void ResetCursorAndSelection()
    {
        _cursorPosition = Math.Min(_cursorPosition, _currentInput.Length);
        ClearSelection();
    }
}

/// <summary>
/// Enumeration of chat states for UI rendering.
/// </summary>
public enum ChatState
{
    Input,
    Thinking,
    ToolExecution
}

/// <summary>
/// Enumeration of message types for consistent rendering.
/// </summary>
public enum MessageType
{
    User,
    Assistant,
    System
}
