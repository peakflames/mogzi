namespace Maxbot.TUI.App;

/// <summary>
/// Simplified TUI application specifically designed for the flex column layout pattern.
/// This bypasses the complex RenderZone system and runs Spectre.Console Live directly.
/// Based on the working FlexColumnPrototype pattern.
/// </summary>
public sealed class FlexColumnTuiApp : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlexColumnTuiApp> _logger;
    private readonly IAnsiConsole _console;
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    
    // Application state
    private bool _isRunning = false;
    private bool _isDisposed = false;
    
    // Chat state
    private ChatState _currentState = ChatState.Input;
    private string _currentInput = string.Empty;
    private string _toolProgress = string.Empty;
    private int _cursorPosition = 0;
    private readonly List<string> _commandHistory = new();
    private int _commandHistoryIndex = -1;

    /// <summary>
    /// Gets whether the application is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Event raised when the application starts.
    /// </summary>
    public event Action? Started;

    /// <summary>
    /// Event raised when the application stops.
    /// </summary>
    public event Action? Stopped;

    /// <summary>
    /// Event raised when an unhandled error occurs.
    /// </summary>
    public event Action<Exception>? UnhandledError;

    /// <summary>
    /// Initializes a new instance of FlexColumnTuiApp.
    /// </summary>
    public FlexColumnTuiApp(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<FlexColumnTuiApp>>();
        _console = serviceProvider.GetService<IAnsiConsole>() ?? AnsiConsole.Console;
        _appService = serviceProvider.GetRequiredService<IAppService>();
        _historyManager = serviceProvider.GetRequiredService<HistoryManager>();
        
        _cancellationTokenSource = new CancellationTokenSource();
        
        // Initialize the advanced keyboard handler
        _keyboardHandler = new AdvancedKeyboardHandler(_serviceProvider.GetService<ILogger<AdvancedKeyboardHandler>>());
        
        // Subscribe to keyboard events
        _keyboardHandler.KeyPressed += OnKeyPressed;
        _keyboardHandler.KeyCombinationPressed += OnKeyCombinationPressed;
        _keyboardHandler.CharacterTyped += OnCharacterTyped;
        
        // Register key bindings
        RegisterKeyBindings();

        _logger.LogDebug("FlexColumnTuiApp initialized");
    }

    /// <summary>
    /// Runs the TUI application with the specified arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(FlexColumnTuiApp));

        if (_isRunning)
            throw new InvalidOperationException("Application is already running");

        try
        {
            _logger.LogInformation("Starting FlexColumn TUI application");
            
            // Combine cancellation tokens
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token);

            // Initialize the application
            Initialize(args);

            // Mark as running
            _isRunning = true;
            Started?.Invoke();

            // Run the main application loop with Spectre.Console Live
            var exitCode = await RunLiveLoopAsync(combinedCts.Token);

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

    /// <summary>
    /// Initializes the application.
    /// </summary>
    private void Initialize(string[] args)
    {
        _logger.LogDebug("Initializing FlexColumn TUI application");

        // Setup console
        _console.Clear();
        
        // Load command history from chat history
        LoadCommandHistory();

        _logger.LogDebug("FlexColumn TUI application initialized");
    }

    /// <summary>
    /// Runs the main application loop using Spectre.Console Live directly.
    /// Based on the working FlexColumnPrototype pattern.
    /// </summary>
    private async Task<int> RunLiveLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting FlexColumn Live loop");

        try
        {
            // Start the keyboard event handling task
            var keyboardTask = _keyboardHandler.StartAsync(cancellationToken);
            
            // Create a single column layout that flows naturally (like flex-col)
            var layout = new Spectre.Console.Layout("Root")
                .SplitRows(
                    new Spectre.Console.Layout("MainContent")
                );

            // Start the Live widget for real-time updates
            await _console.Live(layout)
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // Update the flex column layout
                            var chatHistory = _historyManager.GetCurrentChatHistory();
                            UpdateFlexColumnLayout(layout, chatHistory, _currentState, _currentInput, _toolProgress);
                            ctx.Refresh();

                            // Small delay to prevent excessive CPU usage
                            await Task.Delay(50, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in Live render loop");
                            
                            // Show error in UI
                            var errorPanel = new Panel(new Markup($"[red]Error: {ex.Message}[/]"))
                                .Header("FlexColumn Error")
                                .Border(BoxBorder.Rounded)
                                .BorderColor(Color.Red);
                            layout["MainContent"].Update(errorPanel);
                            ctx.Refresh();
                            
                            await Task.Delay(1000, cancellationToken);
                        }
                    }
                });

            // Stop keyboard handler and wait for completion
            await _keyboardHandler.StopAsync();
            await keyboardTask;

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

    /// <summary>
    /// Registers key bindings for the flex column app.
    /// </summary>
    private void RegisterKeyBindings()
    {
        // Global shortcuts
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.C, ConsoleModifiers.Control, args =>
        {
            _logger.LogInformation("Ctrl+C detected, requesting shutdown");
            _cancellationTokenSource.Cancel();
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.L, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+L detected, clearing screen");
            _console.Clear();
            args.Handled = true;
        });

        // Command history navigation
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

        // External editor placeholder (Ctrl+E)
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.E, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+E detected - external editor feature placeholder");
            // TODO: Implement external editor integration in future phases
            args.Handled = true;
        });

        _logger.LogDebug("FlexColumn key bindings registered");
    }

    /// <summary>
    /// Handles general key press events.
    /// </summary>
    private async void OnKeyPressed(object? sender, KeyPressEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            // Handle navigation and editing keys
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

    /// <summary>
    /// Handles key combination events.
    /// </summary>
    private async void OnKeyCombinationPressed(object? sender, KeyCombinationEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            // Key combinations are handled by registered bindings
            // This event is for any unhandled combinations
            _logger.LogDebug("Unhandled key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles character typed events.
    /// </summary>
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

    /// <summary>
    /// Shuts down the application gracefully.
    /// </summary>
    private async Task ShutdownAsync()
    {
        _logger.LogDebug("Shutting down FlexColumn TUI application");

        try
        {
            // Stop keyboard handler
            await _keyboardHandler.StopAsync();

            // No component to unmount in this simplified approach

            // Clear console
            _console.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }

        _logger.LogDebug("FlexColumn TUI application shutdown complete");
    }

    /// <summary>
    /// Disposes the application and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // Cancel any running operations
        _cancellationTokenSource.Cancel();

        // Dispose keyboard handler
        _keyboardHandler?.Dispose();

        // No component to dispose in this simplified approach

        // Dispose services
        _cancellationTokenSource?.Dispose();

        // Clear event subscriptions
        Started = null;
        Stopped = null;
        UnhandledError = null;

        _logger?.LogDebug("FlexColumnTuiApp disposed");

        GC.SuppressFinalize(this);
    }

    // Input handling methods

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

    /// <summary>
    /// Navigates through command history.
    /// </summary>
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

    /// <summary>
    /// Submits the current input for processing.
    /// </summary>
    private async Task SubmitCurrentInput()
    {
        if (string.IsNullOrWhiteSpace(_currentInput) || _currentState != ChatState.Input) return;
        
        var inputToSubmit = _currentInput;
        AddToCommandHistory(inputToSubmit);
        ClearCurrentInput();
        
        // Process the input
        await ProcessUserInput(inputToSubmit);
    }

    /// <summary>
    /// Adds a command to the history.
    /// </summary>
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

    /// <summary>
    /// Processes user input through the app service.
    /// </summary>
    private async Task ProcessUserInput(string input)
    {
        try
        {
            // Disable input during processing
            _currentState = ChatState.Thinking;
            
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
            _currentState = ChatState.Input;
        }
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
    /// Inserts a character at the current cursor position.
    /// </summary>
    private void InsertCharacter(char character)
    {
        if (_currentState != ChatState.Input) return;
        
        _cursorPosition = Math.Max(0, Math.Min(_cursorPosition, _currentInput.Length));
        _currentInput = _currentInput.Insert(_cursorPosition, character.ToString());
        _cursorPosition++;
        _commandHistoryIndex = -1;
    }

    /// <summary>
    /// Deletes the character before the cursor.
    /// </summary>
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

    /// <summary>
    /// Deletes the character after the cursor.
    /// </summary>
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

    /// <summary>
    /// Moves the cursor left.
    /// </summary>
    private void MoveCursorLeft()
    {
        if (_cursorPosition > 0)
        {
            _cursorPosition--;
        }
    }

    /// <summary>
    /// Moves the cursor right.
    /// </summary>
    private void MoveCursorRight()
    {
        if (_cursorPosition < _currentInput.Length)
        {
            _cursorPosition++;
        }
    }

    /// <summary>
    /// Moves the cursor to the start of the input.
    /// </summary>
    private void MoveCursorToStart()
    {
        _cursorPosition = 0;
    }

    /// <summary>
    /// Moves the cursor to the end of the input.
    /// </summary>
    private void MoveCursorToEnd()
    {
        _cursorPosition = _currentInput.Length;
    }

    /// <summary>
    /// Clears the current input.
    /// </summary>
    private void ClearCurrentInput()
    {
        _currentInput = string.Empty;
        _cursorPosition = 0;
        _commandHistoryIndex = -1;
    }

    /// <summary>
    /// Updates the flex column layout - everything flows in a single column like flex-col.
    /// Based on the working FlexColumnPrototype pattern.
    /// </summary>
    private void UpdateFlexColumnLayout(
        Spectre.Console.Layout layout,
        List<ChatMessage> chatHistory,
        ChatState state,
        string currentInput,
        string toolProgress)
    {
        // Create all content as a single flowing column
        var contentItems = new List<IRenderable>();

        // Add welcome messages if chat is empty
        if (!chatHistory.Any())
        {
            AddWelcomeMessages(contentItems);
        }

        MessageType? prevMessageType = null;

        // Add all chat history with more vertical spacing
        foreach (var message in chatHistory)
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                var messageType = GetMessageType(message);
                
                // Add extra spacing when message types change
                if (prevMessageType != null && prevMessageType != messageType)
                {
                    contentItems.Add(new Text(""));
                }

                var prefix = messageType switch
                {
                    MessageType.User => "[dim]>[/] ",
                    MessageType.Assistant => "✦ ",
                    MessageType.System => "",
                    _ => ""
                };

                var color = messageType switch
                {
                    MessageType.User => "dim",
                    MessageType.Assistant => "white",
                    MessageType.System => "grey69",
                    _ => "white"
                };

                contentItems.Add(new Markup($"[{color}]{prefix}{message.Text}[/]"));

                // Add extra spacing after user messages and AI responses
                if (messageType == MessageType.User || messageType == MessageType.Assistant)
                {
                    contentItems.Add(new Text(""));
                }

                prevMessageType = messageType;
            }
        }

        // Add extra spacing before the bottom component
        contentItems.Add(new Text(""));

        // Add current state component at the bottom (like the last item in flex-col)
        var bottomComponent = state switch
        {
            ChatState.Input => CreateFlexInputComponent(currentInput),
            ChatState.Thinking => CreateFlexThinkingComponent(),
            ChatState.ToolExecution => CreateFlexToolExecutionComponent(toolProgress),
            _ => CreateFlexInputComponent(currentInput)
        };

        contentItems.Add(bottomComponent);

        // Add extra spacing before footer
        contentItems.Add(new Text(""));

        // Add footer
        contentItems.Add(CreateFlexFooterComponent());

        // Create the main content panel with all items flowing vertically
        var contentPanel = new Panel(new Rows(contentItems))
            .Padding(1, 0)
            .NoBorder();

        layout["MainContent"].Update(contentPanel);
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
    /// Creates the input component for flex layout.
    /// </summary>
    private IRenderable CreateFlexInputComponent(string currentInput)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var placeholder = string.IsNullOrEmpty(currentInput) ? "[dim]Type your message or @path/to/file[/]" : "";
        var content = $"{prompt}{currentInput}{cursor}";

        return new Panel(new Markup(content))
            .NoBorder()
            .Expand();
    }

    /// <summary>
    /// Creates the thinking/processing component for flex layout.
    /// </summary>
    private IRenderable CreateFlexThinkingComponent()
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
    private IRenderable CreateFlexToolExecutionComponent(string progress)
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
        
        return new Markup($"[yellow]{leadingAnimation}[/] {progress}");
    }

    /// <summary>
    /// Creates the footer component.
    /// </summary>
    private IRenderable CreateFlexFooterComponent()
    {
        var content = "[dim]~/Desktop/maxbot    no sandbox (see /docs)    gemini-2.5-pro (100% context left)[/]";
        return new Panel(new Markup(content))
            .NoBorder();
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
