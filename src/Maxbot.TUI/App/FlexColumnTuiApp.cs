using MaxBot.PawPrints;
using MaxBot.TUI.Models;

namespace MaxBot.TUI.App;

public sealed class FlexColumnTuiApp : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlexColumnTuiApp> _logger;
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly SlashCommandProcessor _slashCommandProcessor;
    private readonly IScrollbackTerminal _scrollbackTerminal;

    // Application state
    private bool _isRunning = false;
    private bool _isDisposed = false;

    // Chat state
    private ChatState _currentState = ChatState.Input;
    private string _currentInput = string.Empty;
    private string _toolProgress = string.Empty;
    private string _currentToolName = string.Empty;
    private int _cursorPosition = 0;
    private readonly List<string> _commandHistory = new();
    private int _commandHistoryIndex = -1;

    public bool IsRunning => _isRunning;

    public event Action? Started;
    public event Action? Stopped;
    public event Action<Exception>? UnhandledError;

    public FlexColumnTuiApp(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<FlexColumnTuiApp>>();
        _appService = serviceProvider.GetRequiredService<IAppService>();
        _historyManager = serviceProvider.GetRequiredService<HistoryManager>();
        _scrollbackTerminal = serviceProvider.GetRequiredService<IScrollbackTerminal>();

        _cancellationTokenSource = new CancellationTokenSource();

        _keyboardHandler = new AdvancedKeyboardHandler(_serviceProvider.GetService<ILogger<AdvancedKeyboardHandler>>());
        _slashCommandProcessor = new SlashCommandProcessor(AnsiConsole.Console, _serviceProvider.GetService<ILogger<SlashCommandProcessor>>());

        _keyboardHandler.KeyPressed += OnKeyPressed;
        _keyboardHandler.KeyCombinationPressed += OnKeyCombinationPressed;
        _keyboardHandler.CharacterTyped += OnCharacterTyped;

        _slashCommandProcessor.ExitRequested += () => _cancellationTokenSource.Cancel();
        _slashCommandProcessor.ClearHistoryRequested += () =>
        {
            _historyManager.ClearHistory();
            _scrollbackTerminal.Initialize();
            RenderInitialContent();
        };

        RegisterKeyBindings();

        _logger.LogDebug("FlexColumnTuiApp initialized");
    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FlexColumnTuiApp));

        if (_isRunning)
            throw new InvalidOperationException("Application is already running");

        try
        {
            _logger.LogInformation("Starting FlexColumn TUI application");

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            Initialize(args);

            _isRunning = true;
            Started?.Invoke();

            var exitCode = await RunMainLoopAsync(combinedCts.Token);

            _logger.LogInformation("FlexColumn TUI application completed with exit code {ExitCode}", exitCode);
            return exitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("FlexColumn TUI application cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in FlexColumn TUI application");
            UnhandledError?.Invoke(ex);
            return 1;
        }
        finally
        {
            _isRunning = false;
            Stopped?.Invoke();
            await ShutdownAsync();
        }
    }

    private void Initialize(string[] args)
    {
        _logger.LogDebug("Initializing FlexColumn TUI application");
        _scrollbackTerminal.Initialize();
        LoadCommandHistory();
        RenderInitialContent();
        _logger.LogDebug("FlexColumn TUI application initialized");
    }

    private void RenderInitialContent()
    {
        var chatHistory = _historyManager.GetCurrentChatHistory();
        if (!chatHistory.Any())
        {
            _scrollbackTerminal.WriteStatic(CreateWelcomeMessage());
        }
        else
        {
            RenderHistory();
        }
    }

    private void RenderHistory()
    {
        var chatHistory = _historyManager.GetCurrentChatHistory();
        ChatRole? previousRole = null;
        foreach (var message in chatHistory)
        {
            if (previousRole != message.Role)
            {
                _scrollbackTerminal.WriteStatic(new Markup(""));
            }

            _scrollbackTerminal.WriteStatic(RenderMessage(message));

            if (previousRole != message.Role)
            {
                _scrollbackTerminal.WriteStatic(new Markup(""));
            }

            previousRole = message.Role;
        }
    }

    private async Task<int> RunMainLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting FlexColumn Live loop");

        try
        {
            var keyboardTask = _keyboardHandler.StartAsync(cancellationToken);
            var dynamicDisplayTask = _scrollbackTerminal.StartDynamicDisplayAsync(RenderDynamicContent, cancellationToken);

            await Task.WhenAll(keyboardTask, dynamicDisplayTask);

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in FlexColumn Live loop");
            return 1;
        }
    }

    private IRenderable RenderDynamicContent()
    {
        var bottomComponent = _currentState switch
        {
            ChatState.Input => CreateFlexInputComponent(_currentInput),
            ChatState.Thinking => CreateFlexThinkingComponent(),
            ChatState.ToolExecution => CreateFlexToolExecutionComponent(_toolProgress),
            _ => CreateFlexInputComponent(_currentInput)
        };

        return new Rows(new Text(""), bottomComponent, CreateFlexFooterComponent());
    }

    private IRenderable CreateWelcomeMessage()
    {
        var contentItems = new List<IRenderable>
        {
            new Markup("[bold blue]Max[/]"),
            new Text(""),
            new Markup("[grey69]Tips for getting started:[/]"),
            new Markup("[grey69]1. Ask questions, edit files, or run commands.[/]"),
            new Markup("[grey69]2. Be specific for the best results.[/]"),
            new Markup("[grey69]3. Create an .max/AGENT.md files to customize your interactions with Max.[/]"),
            new Markup("[grey69]4. /help for more information.[/]"),
            new Text("")
        };
        return new Rows(contentItems);
    }

    private IRenderable RenderMessage(ChatMessage message)
    {       
        if (string.IsNullOrEmpty(message.Text))
        {
            return new Text(string.Empty);
        }

        var messageType = GetMessageType(message);
        var prefix = messageType switch
        {
            MessageType.User => "[dim]>[/] ",
            MessageType.Assistant => "✦ ",
            _ => ""
        };
        var color = messageType switch
        {
            MessageType.User => "dim",
            MessageType.Assistant => "white",
            _ => "white"
        };

        return new Markup($"[{color}]{prefix}{message.Text}[/]");
    }

    private MessageType GetMessageType(ChatMessage message)
    {
        if (message.Role == ChatRole.User)
        {
            return MessageType.User;
        }
        else if (message.Role == ChatRole.Assistant)
        {
            return MessageType.Assistant;
        }
        else
        {
            return MessageType.System;
        }
    }

    private IRenderable CreateFlexInputComponent(string currentInput)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        
        string content;
        if (string.IsNullOrEmpty(currentInput))
        {
            content = $"{prompt}{cursor}[dim]Type your message or @path/to/file[/]";
        }
        else
        {
            // Insert cursor at the correct position
            var beforeCursor = currentInput.Substring(0, _cursorPosition);
            var afterCursor = currentInput.Substring(_cursorPosition);
            content = $"{prompt}{beforeCursor}{cursor}{afterCursor}";
        }

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey23)
            .Padding(1, 0, 1, 0)
            .Expand();
    }

    private IRenderable CreateFlexThinkingComponent()
    {
        var animationFrame = DateTime.Now.Millisecond / 250 % 4;
        var leadingAnimation = animationFrame switch
        {
            0 => "   ",
            1 => ".  ",
            2 => ".. ",
            3 => "...",
            _ => "   "
        };

        return new Panel(new Markup($"[orange3]{leadingAnimation}[/] [dim]API Request...[/]"))
            .NoBorder();
    }

    private IRenderable CreateFlexToolExecutionComponent(string progress)
    {
        var animationFrame = DateTime.Now.Millisecond / 250 % 4;
        var leadingAnimation = animationFrame switch
        {
            0 => "   ",
            1 => ".  ",
            2 => ".. ",
            3 => "...",
            _ => "   "
        };

        // Provide meaningful progress text
        var progressText = string.IsNullOrWhiteSpace(progress) 
            ? (!string.IsNullOrWhiteSpace(_currentToolName) 
                ? $"Executing {_currentToolName}..." 
                : "Executing tool...")
            : progress;

        return new Panel(new Markup($"[yellow]{leadingAnimation}[/] [dim]{progressText}[/]"))
            .NoBorder();
    }

    private IRenderable CreateFlexFooterComponent()
    {
        var content = "[dim]~/Desktop/maxbot    no sandbox (see /docs)    gemini-2.5-pro (100% context left)[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }

    private void RegisterKeyBindings()
    {
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.C, ConsoleModifiers.Control, args =>
        {
            _logger.LogInformation("Ctrl+C detected, requesting shutdown");
            _cancellationTokenSource.Cancel();
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.L, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+L detected, clearing screen");
            _historyManager.ClearHistory();
            _scrollbackTerminal.Initialize();
            RenderInitialContent();
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.P, ConsoleModifiers.Control, args =>
        {
            NavigateCommandHistory(up: true);
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.N, ConsoleModifiers.Control, args =>
        {
            NavigateCommandHistory(up: false);
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.E, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+E detected - external editor feature placeholder");
            args.Handled = true;
        });

        _logger.LogDebug("FlexColumn key bindings registered");
    }

    private async void OnKeyPressed(object? sender, KeyPressEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            switch (e.Key)
            {
                case ConsoleKey.Enter:
                    await SubmitCurrentInput();
                    e.Handled = true;
                    break;

                case ConsoleKey.UpArrow:
                    NavigateCommandHistory(up: true);
                    e.Handled = true;
                    break;

                case ConsoleKey.DownArrow:
                    NavigateCommandHistory(up: false);
                    e.Handled = true;
                    break;

                case ConsoleKey.LeftArrow:
                    MoveCursorLeft();
                    e.Handled = true;
                    break;

                case ConsoleKey.RightArrow:
                    MoveCursorRight();
                    e.Handled = true;
                    break;

                case ConsoleKey.Home:
                    MoveCursorToStart();
                    e.Handled = true;
                    break;

                case ConsoleKey.End:
                    MoveCursorToEnd();
                    e.Handled = true;
                    break;

                case ConsoleKey.Backspace:
                    DeleteCharacterBefore();
                    e.Handled = true;
                    break;

                case ConsoleKey.Delete:
                    DeleteCharacterAfter();
                    e.Handled = true;
                    break;

                case ConsoleKey.Escape:
                    ClearCurrentInput();
                    e.Handled = true;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key press: {Key}", e.Key);
        }
    }

    private async void OnKeyCombinationPressed(object? sender, KeyCombinationEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            _logger.LogDebug("Unhandled key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }

        await Task.CompletedTask;
    }

    private async void OnCharacterTyped(object? sender, CharacterTypedEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            InsertCharacter(e.Character);
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character typed: {Character}", e.Character);
        }

        await Task.CompletedTask;
    }

    private async Task ShutdownAsync()
    {
        _logger.LogDebug("Shutting down FlexColumn TUI application");

        try
        {
            await _keyboardHandler.StopAsync();
            _scrollbackTerminal.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }

        _logger.LogDebug("FlexColumn TUI application shutdown complete");
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        _cancellationTokenSource.Cancel();
        _keyboardHandler?.Dispose();
        _cancellationTokenSource?.Dispose();

        Started = null;
        Stopped = null;
        UnhandledError = null;

        _logger?.LogDebug("FlexColumnTuiApp disposed");

        GC.SuppressFinalize(this);
    }

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

    private void NavigateCommandHistory(bool up)
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

        _cursorPosition = _currentInput.Length;
    }

    private async Task SubmitCurrentInput()
    {
        if (string.IsNullOrWhiteSpace(_currentInput) || _currentState != ChatState.Input) return;

        var inputToSubmit = _currentInput;
        AddToCommandHistory(inputToSubmit);
        ClearCurrentInput();

        await ProcessUserInput(inputToSubmit);
    }

    private void AddToCommandHistory(string command)
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

    private async Task ProcessUserInput(string input)
    {
        try
        {
            var userMessage = new ChatMessage(ChatRole.User, input);
            _historyManager.AddUserMessage(userMessage);
            _scrollbackTerminal.WriteStatic(RenderMessage(userMessage));
            _scrollbackTerminal.WriteStatic(new Markup(""));
            if (_slashCommandProcessor.TryProcessCommand(input, out var commandOutput))
            {
                if (!string.IsNullOrEmpty(commandOutput))
                {
                    var commandMessage = new ChatMessage(ChatRole.Assistant, commandOutput);
                    _historyManager.AddAssistantMessage(commandMessage);
                    _scrollbackTerminal.WriteStatic(RenderMessage(commandMessage));
                }
                return;
            }

            _currentState = ChatState.Thinking;

            var chatHistory = _historyManager.GetCurrentChatHistory();
            var responseStream = _appService.ProcessChatMessageAsync(chatHistory, CancellationToken.None);

            var assistantMessage = new ChatMessage(ChatRole.Assistant, "");
            _historyManager.AddAssistantMessage(assistantMessage);
            _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage), isUpdatable: true);

            await foreach (var responseUpdate in responseStream)
            {
                if (!string.IsNullOrEmpty(responseUpdate.Text))
                {
                    var newText = assistantMessage.Text + responseUpdate.Text;
                    assistantMessage = new ChatMessage(ChatRole.Assistant, newText);
                    _historyManager.UpdateLastMessage(assistantMessage);
                    _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage), isUpdatable: true);
                }

                if (IsToolExecutionUpdate(responseUpdate))
                {
                    _currentState = ChatState.ToolExecution;

                    // Extract tool name first
                    ExtractToolNameFromUpdate(responseUpdate);

                    // Set progress text based on available information
                    if (!string.IsNullOrWhiteSpace(responseUpdate.Text))
                    {
                        _toolProgress = responseUpdate.Text;
                    }
                    else if (!string.IsNullOrWhiteSpace(_currentToolName))
                    {
                        _toolProgress = $"Executing {_currentToolName}...";
                        _scrollbackTerminal.WriteStatic(new Markup($":green_circle: [dim] {_toolProgress}[/]"));
                    }
                }
            }
            _scrollbackTerminal.WriteStatic(new Markup(""));
            _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing user input");
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing input: {ex.Message}");
            _historyManager.AddAssistantMessage(errorMessage);
            _scrollbackTerminal.WriteStatic(RenderMessage(errorMessage));
        }
        finally
        {
            _currentState = ChatState.Input;
            _toolProgress = string.Empty;
            _currentToolName = string.Empty;
        }
    }

    private bool IsToolExecutionUpdate(ChatResponseUpdate responseUpdate)
    {
        if (responseUpdate.Contents == null) return false;

        return responseUpdate.Contents.Any(content =>
            content is FunctionCallContent || content is FunctionResultContent);
    }

    private void ExtractToolNameFromUpdate(ChatResponseUpdate responseUpdate)
    {
        if (responseUpdate.Contents == null) return;

        var functionCall = responseUpdate.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall != null)
        {
            var toolName = functionCall.Name ?? "Unknown Tool";

            // Include function call arguments if available
            if (functionCall.Arguments != null && functionCall.Arguments.Count > 0)
            {
                var argsList = new List<string>();
                foreach (var arg in functionCall.Arguments)
                {
                    var valueStr = arg.Value?.ToString() ?? "null";
                    // Truncate long values to keep the display manageable
                    if (valueStr.Length > 50)
                    {
                        valueStr = valueStr.Substring(0, 47) + "...";
                    }
                    argsList.Add($"{arg.Key}={valueStr}");
                }
                _currentToolName = $"{toolName}({string.Join(", ", argsList)})";
            }
            else
            {
                _currentToolName = $"{toolName}()";
            }
        }
        else
        {
            _currentToolName = string.Empty;
        }
    }

    private void InsertCharacter(char character)
    {
        if (_currentState != ChatState.Input) return;

        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        _currentInput = _currentInput.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        _commandHistoryIndex = -1;
    }

    private void DeleteCharacterBefore()
    {
        if (_currentState != ChatState.Input || _currentInput.Length == 0) return;

        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));

        if (_cursorPosition > 0)
        {
            _currentInput = _currentInput.Remove(_cursorPosition - 1, 1);
            _cursorPosition--;
        }

        _commandHistoryIndex = -1;
    }

    private void DeleteCharacterAfter()
    {
        if (_currentState != ChatState.Input || _currentInput.Length == 0) return;

        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));

        if (_cursorPosition < _currentInput.Length)
        {
            _currentInput = _currentInput.Remove(_cursorPosition, 1);
        }

        _commandHistoryIndex = -1;
    }

    private void MoveCursorLeft()
    {
        if (_cursorPosition > 0)
        {
            _cursorPosition--;
        }
    }

    private void MoveCursorRight()
    {
        if (_cursorPosition < _currentInput.Length)
        {
            _cursorPosition++;
        }
    }

    private void MoveCursorToStart()
    {
        _cursorPosition = 0;
    }

    private void MoveCursorToEnd()
    {
        _cursorPosition = _currentInput.Length;
    }

    private void ClearCurrentInput()
    {
        _currentInput = string.Empty;
        _cursorPosition = 0;
        _commandHistoryIndex = -1;
    }
}
