# MaxBot Terminal Interface & TUI Design

## Terminal Interface Architecture

MaxBot's Terminal User Interface (TUI) provides a sophisticated, responsive interface for AI-powered development assistance. The design emphasizes real-time interaction, efficient rendering, and seamless integration with the underlying chat and tool systems.

## Core Interface Components

### IScrollbackTerminal Interface

**Primary Interface Definition:**
```csharp
public interface IScrollbackTerminal
{
    void Initialize();
    void WriteStatic(IRenderable content, bool isUpdatable = false);
    Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken);
    void Shutdown();
}
```

**Design Principles:**
- **Content Separation**: Clear distinction between static, updatable, and dynamic content
- **Async Operations**: Non-blocking dynamic content updates
- **Resource Management**: Proper initialization and cleanup lifecycle
- **Thread Safety**: Safe concurrent access from multiple components

### ScrollbackTerminal Implementation

**Core Implementation Structure:**
```csharp
public class ScrollbackTerminal : IScrollbackTerminal
{
    private readonly IAnsiConsole _console;
    private readonly object _lock = new();
    private readonly List<IRenderable> _staticContent = [];
    private readonly List<IRenderable> _updatableContent = [];
    
    private int _staticContentLineCount = 0;
    private int _updatableContentLineCount = 0;
    private int _dynamicContentLineCount = 0;
    private bool _isInitialized = false;
    private bool _isDynamicMode = false;

    public ScrollbackTerminal(IAnsiConsole console)
    {
        _console = console;
    }
}
```

**Key Features:**
- **Thread Safety**: Lock-based synchronization for concurrent access
- **Content Management**: Separate tracking of static, updatable, and dynamic content
- **Cursor Management**: Hide/show cursor during operations
- **Content Clearing**: Efficient ANSI escape sequences for content updates
- **Line Counting**: Accurate line counting for proper cursor positioning

## Content Management System

### Static Content Handling

**Static Content Operations:**
```csharp
public void WriteStatic(IRenderable content, bool isUpdatable = false)
{
    lock (_lock)
    {
        if (_isDynamicMode)
        {
            throw new InvalidOperationException("Cannot write static content while in dynamic mode");
        }

        if (isUpdatable)
        {
            _updatableContent.Add(content);
            _updatableContentLineCount += CountLines(content);
        }
        else
        {
            _staticContent.Add(content);
            _staticContentLineCount += CountLines(content);
        }

        _console.Write(content);
    }
}
```

### Dynamic Content Updates

**Real-Time Content Rendering:**
```csharp
public async Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken)
{
    lock (_lock)
    {
        if (_isDynamicMode)
        {
            throw new InvalidOperationException("Already in dynamic mode");
        }
        
        _isDynamicMode = true;
        _console.Cursor.Hide();
    }

    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var content = dynamicContentProvider();
            UpdateDynamic(content);
            
            await Task.Delay(50, cancellationToken); // 20 FPS update rate
        }
    }
    finally
    {
        lock (_lock)
        {
            _isDynamicMode = false;
            _console.Cursor.Show();
        }
    }
}

private void UpdateDynamic(IRenderable content)
{
    lock (_lock)
    {
        ClearDynamicContent();
        
        // Measure content for line counting
        var lineCount = CountLines(content);
        _dynamicContentLineCount = lineCount;
        
        _console.Write(content);
    }
}
```

### Content Clearing Strategy

**Efficient Content Removal:**
```csharp
private void ClearDynamicContent()
{
    if (_dynamicContentLineCount > 0)
    {
        // Move cursor up to the beginning of dynamic content
        _console.Write($"\u001b[{_dynamicContentLineCount}A");
        
        // Clear from cursor to end of screen
        _console.Write("\u001b[0J");
        
        _dynamicContentLineCount = 0;
    }
}

private void ClearUpdatableContent()
{
    if (_updatableContentLineCount > 0)
    {
        // Move cursor up and clear updatable content
        _console.Write($"\u001b[{_updatableContentLineCount}A");
        _console.Write("\u001b[0J");
        
        _updatableContentLineCount = 0;
        _updatableContent.Clear();
    }
}
```

## TUI Application Design

### Dependency Injection Setup

**Service Configuration:**
```csharp
private static void ConfigureServices(IServiceCollection services, string[] args)
{
    services.AddLogging(builder => {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });
    
    services.AddSingleton<IAnsiConsole>(AnsiConsole.Console);
    services.AddSingleton<IWorkingDirectoryProvider, DefaultWorkingDirectoryProvider>();
    
    var chatClientResult = ChatClient.Create("maxbot.config.json", null, null, "chat", (details, color) => {}, false);
    if (chatClientResult.IsSuccess)
        services.AddSingleton(chatClientResult.Value);
    
    services.AddSingleton<IAppService, AppService>();
    services.AddSingleton<HistoryManager>();
    services.AddSingleton<StateManager>();
    services.AddSingleton<FlexColumnTuiApp>();
    services.AddSingleton<IScrollbackTerminal, ScrollbackTerminal>();
    
    // Autocomplete services
    services.AddSingleton<AutocompleteManager>();
    services.AddSingleton<IAutocompleteProvider, FilePathProvider>();
    services.AddSingleton<IAutocompleteProvider, SlashCommandProvider>();
}
```

### FlexColumnTuiApp Architecture

**Main Application Structure:**
```csharp
public class FlexColumnTuiApp
{
    private readonly IAppService _appService;
    private readonly IScrollbackTerminal _terminal;
    private readonly StateManager _stateManager;
    private readonly HistoryManager _historyManager;
    private readonly AutocompleteManager _autocompleteManager;
    private readonly AdvancedKeyboardHandler _keyboardHandler;

    public FlexColumnTuiApp(
        IAppService appService,
        IScrollbackTerminal terminal,
        StateManager stateManager,
        HistoryManager historyManager,
        AutocompleteManager autocompleteManager)
    {
        _appService = appService;
        _terminal = terminal;
        _stateManager = stateManager;
        _historyManager = historyManager;
        _autocompleteManager = autocompleteManager;
        _keyboardHandler = new AdvancedKeyboardHandler(_autocompleteManager);
    }
}
```

### Application Lifecycle

**Startup and Execution Flow:**
```csharp
public async Task RunAsync(CancellationToken cancellationToken = default)
{
    try
    {
        _terminal.Initialize();
        
        // Display welcome message
        DisplayWelcomeMessage();
        
        // Main application loop
        await MainLoopAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Graceful shutdown
    }
    catch (Exception ex)
    {
        DisplayError($"Application error: {ex.Message}");
    }
    finally
    {
        _terminal.Shutdown();
    }
}

private async Task MainLoopAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        try
        {
            // Handle user input
            var inputResult = await HandleUserInputAsync(cancellationToken);
            
            if (inputResult.ShouldExit)
                break;
                
            if (!string.IsNullOrWhiteSpace(inputResult.Input))
            {
                await ProcessUserMessageAsync(inputResult.Input, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            DisplayError($"Error processing input: {ex.Message}");
        }
    }
}
```

## State Management Design

### ChatState Enumeration

**Application State Definition:**
```csharp
public enum ChatState
{
    Input,          // Ready for user input
    Thinking,       // AI processing request
    ToolExecution   // Tool execution in progress
}
```

### StateManager Implementation

**Centralized State Control:**
```csharp
public class StateManager
{
    private ChatState _currentState = ChatState.Input;
    private readonly object _stateLock = new();
    
    public event EventHandler<ChatState>? StateChanged;

    public ChatState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public void SetState(ChatState newState)
    {
        lock (_stateLock)
        {
            if (_currentState != newState)
            {
                var oldState = _currentState;
                _currentState = newState;
                
                StateChanged?.Invoke(this, newState);
            }
        }
    }

    public bool TryTransition(ChatState from, ChatState to)
    {
        lock (_stateLock)
        {
            if (_currentState == from)
            {
                _currentState = to;
                StateChanged?.Invoke(this, to);
                return true;
            }
            return false;
        }
    }
}
```

### HistoryManager Design

**Session Management:**
```csharp
public class HistoryManager
{
    private readonly IAppService _appService;
    private List<ChatMessage> _currentHistory = [];
    private string? _currentSessionId;

    public HistoryManager(IAppService appService)
    {
        _appService = appService;
    }

    public async Task<bool> LoadSessionAsync(string sessionId)
    {
        try
        {
            var history = await _appService.LoadChatSessionAsync(sessionId, _appService.SystemPrompt);
            if (history != null)
            {
                _currentHistory = history;
                _currentSessionId = sessionId;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string CreateNewSession()
    {
        _currentSessionId = _appService.CreateChatSession();
        _currentHistory = [new ChatMessage(ChatRole.System, _appService.SystemPrompt)];
        return _currentSessionId;
    }

    public async Task SaveCurrentSessionAsync()
    {
        if (_currentSessionId != null && _currentHistory.Count > 1)
        {
            var sessionPath = Path.Combine(_appService.GetChatSessionsBasePath(), _currentSessionId, "chat_history.json");
            await _appService.SaveChatHistoryAsync(sessionPath, _currentHistory);
        }
    }
}
```

## Input Handling System

### AdvancedKeyboardHandler

**Keyboard Event Processing:**
```csharp
public class AdvancedKeyboardHandler
{
    private readonly AutocompleteManager _autocompleteManager;
    private readonly InputContext _inputContext;

    public AdvancedKeyboardHandler(AutocompleteManager autocompleteManager)
    {
        _autocompleteManager = autocompleteManager;
        _inputContext = new InputContext();
    }

    public async Task<InputResult> HandleInputAsync(CancellationToken cancellationToken)
    {
        var input = new StringBuilder();
        var cursorPosition = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var keyInfo = Console.ReadKey(true);
            
            // Handle autocomplete navigation first
            if (_inputContext.HasSuggestions && HandleAutocompleteNavigation(keyInfo))
            {
                continue;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    if (_inputContext.HasSuggestions && _inputContext.SelectedSuggestionIndex >= 0)
                    {
                        // Accept autocomplete suggestion
                        _autocompleteManager.AcceptSuggestion(_inputContext);
                        input.Clear();
                        input.Append(_inputContext.Input);
                        cursorPosition = _inputContext.CursorPosition;
                        continue;
                    }
                    return new InputResult { Input = input.ToString(), ShouldExit = false };

                case ConsoleKey.Escape:
                    if (_inputContext.HasSuggestions)
                    {
                        // Clear autocomplete suggestions
                        _inputContext.ClearSuggestions();
                        continue;
                    }
                    return new InputResult { Input = string.Empty, ShouldExit = true };

                case ConsoleKey.Backspace:
                    if (cursorPosition > 0)
                    {
                        input.Remove(cursorPosition - 1, 1);
                        cursorPosition--;
                        await UpdateAutocompleteAsync(input.ToString(), cursorPosition);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPosition < input.Length)
                    {
                        input.Remove(cursorPosition, 1);
                        await UpdateAutocompleteAsync(input.ToString(), cursorPosition);
                    }
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        input.Insert(cursorPosition, keyInfo.KeyChar);
                        cursorPosition++;
                        await UpdateAutocompleteAsync(input.ToString(), cursorPosition);
                    }
                    break;
            }

            // Update display
            UpdateInputDisplay(input.ToString(), cursorPosition);
        }

        return new InputResult { Input = string.Empty, ShouldExit = true };
    }

    private async Task UpdateAutocompleteAsync(string input, int cursorPosition)
    {
        _inputContext.Input = input;
        _inputContext.CursorPosition = cursorPosition;
        
        await _autocompleteManager.UpdateSuggestionsAsync(_inputContext);
    }
}
```

## Rendering System

### Content Composition

**Composite Rendering Pattern:**
```csharp
public class ContentRenderer
{
    private readonly IScrollbackTerminal _terminal;
    private readonly StateManager _stateManager;

    public void RenderChatMessage(ChatMessage message)
    {
        var panel = new Panel(new Markup(message.Content))
        {
            Header = new PanelHeader($"[bold]{message.Role}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = GetStyleForRole(message.Role)
        };

        _terminal.WriteStatic(panel);
    }

    public void RenderInputPrompt(string input, int cursorPosition, List<CompletionItem>? suggestions = null)
    {
        var promptContent = new Rows(
            new Text($"> {input}"),
            RenderCursor(cursorPosition),
            RenderSuggestions(suggestions)
        );

        _terminal.WriteStatic(promptContent, isUpdatable: true);
    }

    private IRenderable RenderSuggestions(List<CompletionItem>? suggestions)
    {
        if (suggestions == null || suggestions.Count == 0)
            return new Text(string.Empty);

        var suggestionRows = suggestions.Select((item, index) =>
        {
            var style = index == 0 ? "[bold yellow]" : "[dim]";
            return new Text($"{style}{item.DisplayText}[/] - {item.Description}");
        });

        return new Rows(suggestionRows);
    }

    private Style GetStyleForRole(ChatRole role)
    {
        return role switch
        {
            ChatRole.User => Style.Parse("blue"),
            ChatRole.Assistant => Style.Parse("green"),
            ChatRole.System => Style.Parse("yellow"),
            _ => Style.Parse("white")
        };
    }
}
```

### Performance Optimizations

**Efficient Rendering Strategies:**
```csharp
private int CountLines(IRenderable content)
{
    // Use a measuring console to count lines without rendering
    var writer = new StringWriter();
    var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings 
    { 
        Out = new AnsiConsoleOutput(writer), 
        ColorSystem = ColorSystemSupport.NoColors 
    });
    
    measuringConsole.Write(content);
    var output = writer.ToString();
    
    // Count actual line breaks
    var lines = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
    return Math.Max(1, lines.Length);
}

private void OptimizedContentUpdate(IRenderable newContent)
{
    // Only update if content has actually changed
    var newHash = GetContentHash(newContent);
    if (newHash != _lastContentHash)
    {
        UpdateDynamic(newContent);
        _lastContentHash = newHash;
    }
}
```

## Error Handling and Recovery

### Terminal Error Management

**Graceful Error Handling:**
```csharp
public class TerminalErrorHandler
{
    private readonly IScrollbackTerminal _terminal;
    private readonly ILogger _logger;

    public void HandleTerminalError(Exception ex, string context)
    {
        try
        {
            var errorPanel = new Panel(new Text($"[red]Error in {context}: {ex.Message}[/]"))
            {
                Header = new PanelHeader("[red bold]Error[/]"),
                Border = BoxBorder.Heavy,
                BorderStyle = Style.Parse("red")
            };

            _terminal.WriteStatic(errorPanel);
            _logger.LogError(ex, "Terminal error in context: {Context}", context);
        }
        catch
        {
            // Fallback to console output if terminal is corrupted
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
        }
    }

    public void HandleRecovery()
    {
        try
        {
            // Attempt to reset terminal state
            Console.Clear();
            Console.CursorVisible = true;
            
            _terminal.Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to recover terminal: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
```

## Integration Points

### Autocomplete Integration

**Seamless Autocomplete Rendering:**
```csharp
public void RenderAutocompleteOverlay(InputContext context)
{
    if (!context.HasSuggestions)
        return;

    var suggestions = context.Suggestions.Take(5).Select((item, index) =>
    {
        var prefix = index == context.SelectedSuggestionIndex ? "► " : "  ";
        var style = index == context.SelectedSuggestionIndex ? "[yellow bold]" : "[dim]";
        
        return new Text($"{prefix}{style}{item.DisplayText}[/] - {item.Description}");
    });

    var overlay = new Panel(new Rows(suggestions))
    {
        Header = new PanelHeader("[blue]Suggestions[/]"),
        Border = BoxBorder.Rounded,
        BorderStyle = Style.Parse("blue")
    };

    _terminal.WriteStatic(overlay, isUpdatable: true);
}
```

### State-Driven UI Updates

**Reactive UI Based on Application State:**
```csharp
private void OnStateChanged(object? sender, ChatState newState)
{
    switch (newState)
    {
        case ChatState.Input:
            ShowInputPrompt();
            break;
            
        case ChatState.Thinking:
            ShowThinkingIndicator();
            break;
            
        case ChatState.ToolExecution:
            ShowToolExecutionIndicator();
            break;
    }
}

private void ShowThinkingIndicator()
{
    var spinner = new Spinner(Spinner.Known.Dots)
    {
        Style = Style.Parse("yellow")
    };
    
    _terminal.StartDynamicDisplayAsync(() => 
        new Columns(
            new Text("[yellow]AI is thinking...[/]"),
            spinner
        ), CancellationToken.None);
}
```

This terminal interface and TUI design provides MaxBot with a sophisticated, responsive user interface that seamlessly integrates with the autocomplete system, state management, and AI processing capabilities while maintaining excellent performance and user experience.
