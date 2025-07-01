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
    private readonly AutocompleteManager _autocompleteManager;
    private readonly IScrollbackTerminal _scrollbackTerminal;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;
    private readonly ToolResponseParser _toolResponseParser;
    private bool _isDisposed = false;

    // Chat state
    private ChatState _currentState = ChatState.Input;
    private readonly InputContext _inputContext = new();
    private string _toolProgress = string.Empty;
    private string _currentToolName = string.Empty;
    private readonly List<string> _commandHistory = [];
    private int _commandHistoryIndex = -1;

    // AI operation management
    private CancellationTokenSource? _aiOperationCts;
    private DateTime _aiOperationStartTime;

    // Tool execution tracking
    private readonly Dictionary<string, string> _functionCallToToolName = new();
    private readonly Dictionary<string, string> _functionCallToPreEditContent = new();

    public bool IsRunning { get; private set; } = false;

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
        _workingDirectoryProvider = serviceProvider.GetRequiredService<IWorkingDirectoryProvider>();
        _toolResponseParser = serviceProvider.GetRequiredService<ToolResponseParser>();

        _cancellationTokenSource = new CancellationTokenSource();

        _keyboardHandler = new AdvancedKeyboardHandler(_serviceProvider.GetService<ILogger<AdvancedKeyboardHandler>>());
        _slashCommandProcessor = new SlashCommandProcessor(AnsiConsole.Console, _serviceProvider.GetService<ILogger<SlashCommandProcessor>>(), _serviceProvider.GetService<ChatClient>());
        _autocompleteManager = serviceProvider.GetRequiredService<AutocompleteManager>();

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
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Application is already running");
        }

        try
        {
            _logger.LogInformation("Starting FlexColumn TUI application");

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            Initialize(args);

            IsRunning = true;
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
            IsRunning = false;
            Stopped?.Invoke();
            await ShutdownAsync();
        }
    }

#pragma warning disable IDE0060 // Remove unused parameter
    private void Initialize(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
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

            // TODO: Implement once session management is implemented
            // _scrollbackTerminal.WriteStatic(new Markup("[dim]No existing chat history found, starting fresh session[/]"));
            // _scrollbackTerminal.WriteStatic(new Markup(""));
        }
        else
        {
            // TODO: Implement once session management is implemented
            // _scrollbackTerminal.WriteStatic(new Markup($"[dim]Loading {chatHistory.Count} messages from existing chat history[/]"));
            // _scrollbackTerminal.WriteStatic(new Markup(""));
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
            ChatState.Input when _inputContext.ShowSuggestions => 
                CreateInputWithAutocomplete(_inputContext),
            ChatState.Input => 
                CreateFlexInputComponent(_inputContext.CurrentInput),
            ChatState.Thinking => 
                CreateFlexThinkingComponent(),
            ChatState.ToolExecution => 
                CreateFlexToolExecutionComponent(_toolProgress),
            _ => CreateFlexInputComponent(_inputContext.CurrentInput)
        };

        return new Rows(new Text(""), bottomComponent, new Text(""), CreateFlexFooterComponent());
    }


    private IRenderable CreateWelcomeMessage()
    {
        var contentItems = new List<IRenderable>
        {
            // Shadow effect style with multi-color letters - "Max" with each letter different color
            new Markup("[bold blue]███╗   ███╗[/] [bold cyan] █████╗ [/] [bold magenta]██╗  ██╗[/]"),
            new Markup("[bold blue]████╗ ████║[/] [bold cyan]██╔══██╗[/] [bold magenta]╚██╗██╔╝[/]"),
            new Markup("[bold blue]██╔████╔██║[/] [bold cyan]███████║[/] [bold magenta] ╚███╔╝ [/]"),
            new Markup("[bold blue]██║╚██╔╝██║[/] [bold cyan]██╔══██║[/] [bold magenta] ██╔██╗ [/]"),
            new Markup("[bold blue]██║ ╚═╝ ██║[/] [bold cyan]██║  ██║[/] [bold magenta]██╔╝ ██╗[/]"),
            new Markup("[bold blue]╚═╝     ╚═╝[/] [bold cyan]╚═╝  ╚═╝[/] [bold magenta]╚═╝  ╚═╝[/]"),
            new Text(""),
            new Markup("[bold cyan]◢◤◢◤◢◤ Now connected to your Multi-model Autonomous eXpert ◢◤◢◤◢◤[/]"),
            new Text(""),
            new Markup("[dim]Your AI-powered development assistant[/]"),
            new Text(""),
            new Markup("[grey69]Tips for getting started:[/]"),
            new Markup("[grey69]1. Ask questions, edit files, or run commands[/]"),
            new Markup("[grey69]2. Be specific for the best results[/]"),
            new Markup("[grey69]3. Use [/][magenta]/help[/][dim] for more information[/]"),
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
            content = $"{prompt}{cursor}[dim]Type your message or /help[/]";
        }
        else
        {
            // Insert cursor at the correct position
            var beforeCursor = currentInput[.._inputContext.CursorPosition];
            var afterCursor = currentInput[_inputContext.CursorPosition..];
            content = $"{prompt}{beforeCursor}{cursor}{afterCursor}";
        }

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey23)
            .Padding(1, 0, 1, 0)
            .Expand();
    }

    private IRenderable CreateInputWithAutocomplete(InputContext context)
    {
        var inputPanel = CreateFlexInputComponent(context.CurrentInput);

        if (!context.ShowSuggestions || context.Suggestions.Count == 0)
        {
            return inputPanel;
        }

        var suggestionItems = context.Suggestions.Select((suggestion, index) => 
        {
            var isSelected = index == context.SelectedSuggestionIndex;
            var style = isSelected ? "[blue on white]" : "[dim]";
            var prefix = isSelected ? ">" : " ";

            var description = _slashCommandProcessor.GetAllCommands()
                .GetValueOrDefault(suggestion, "");

            return new Markup($"{style}{prefix} {suggestion,-12} {description}[/]");
        }).ToArray();

        var suggestionsPanel = new Panel(new Rows(suggestionItems))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(0, 0);

        return new Rows(inputPanel, suggestionsPanel);
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

        // Calculate duration since AI operation started
        var duration = (int)(DateTime.Now - _aiOperationStartTime).TotalSeconds;
        var durationText = duration > 0 ? $"{duration}s" : "0s";

        return new Panel(new Markup($"[orange3]{leadingAnimation}[/] [dim]Thinking (esc to cancel, {durationText})[/]"))
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
        var currentDir = GetDisplayPath(_workingDirectoryProvider.GetCurrentDirectory());
        var modelInfo = GetModelDisplayInfo();
        var tokenInfo = GetTokenUsageInfo();
        var content = $"[skyblue2]{currentDir}[/]  [rosybrown]{modelInfo}[/] [dim]({tokenInfo})[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }

    private string GetTokenUsageInfo()
    {
        try
        {
            var chatHistory = _historyManager.GetCurrentChatHistory();
            var tokenCount = _appService.CalculateTokenMetrics(chatHistory);

            // Estimate context window size based on model (this could be made configurable)
            var contextWindowSize = EstimateContextWindowSize();
            var percentageUsed = Math.Min(100.0, tokenCount * 100.0 / contextWindowSize);
            var percentageLeft = 100.0 - percentageUsed;

            return $"{tokenCount:N0} tokens, {percentageLeft:F2}% context left";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error calculating token usage");
            return "token calculation unavailable";
        }
    }

    private int EstimateContextWindowSize()
    {
        try
        {
            var modelId = _appService.ChatClient.ActiveProfile.ModelId.ToLowerInvariant();

            // Common model context window sizes
            return modelId switch
            {
                var m when m.Contains("gpt-4.1") => 1047576, // GPT-4.1 models
                var m when m.Contains("gpt-4") => 128000,  // GPT-4 Turbo models
                var m when m.Contains("gpt-3.5-") => 16385, // GPT-3.5 Turbo models
                var m when m.Contains("o3") => 200000, // o3 models
                var m when m.Contains("o4") => 200000, // o4 models
                var m when m.Contains("gemini-2.5") => 1048576, // Gemini 2.5 Flash models
                var m when m.Contains("gemini-1.5") => 1048576, // Gemini 1.5 Pro models
                var m when m.Contains("claude") => 200000, // Claude models
                _ => 128000 // Default fallback
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error estimating context window size");
            return 128000; // Safe default
        }
    }

    private string GetModelDisplayInfo()
    {
        try
        {
            var chatClient = _appService.ChatClient;
            var provider = chatClient.ActiveApiProvider.Name;
            var model = chatClient.ActiveProfile.ModelId;

            // Format like "provider/model" or just "model" if provider is empty
            if (!string.IsNullOrEmpty(provider))
            {
                return $"{provider}:{model}";
            }
            return model;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error getting model display info");
            return "unknown-model";
        }
    }

    private string GetDisplayPath(string fullPath)
    {
        try
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (fullPath.StartsWith(homeDir))
            {
                return "~" + fullPath[homeDir.Length..].Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error formatting display path for {Path}", fullPath);
            return fullPath;
        }
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
        if (e.Handled)
        {
            return;
        }

        try
        {
            // Handle autocomplete navigation first
            if (_inputContext.State == InputState.Autocomplete && _inputContext.ShowSuggestions)
            {
#pragma warning disable IDE0010 // Add missing cases
                switch (e.Key)
                {
                    case ConsoleKey.UpArrow:
                        NavigateAutocomplete(up: true);
                        e.Handled = true;
                        return;

                    case ConsoleKey.DownArrow:
                        NavigateAutocomplete(up: false);
                        e.Handled = true;
                        return;

                    case ConsoleKey.Tab:
                    case ConsoleKey.Enter:
                        AcceptAutocompleteSuggestion();
                        e.Handled = true;
                        return;

                    case ConsoleKey.Escape:
                        CancelAutocomplete();
                        e.Handled = true;
                        return;
                }
#pragma warning restore IDE0010 // Add missing cases
            }

#pragma warning disable IDE0010 // Add missing cases
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
                    HandleEscapeKey();
                    e.Handled = true;
                    break;
                default:
                    break;
            }
#pragma warning restore IDE0010 // Add missing cases
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key press: {Key}", e.Key);
        }
    }

    private async void OnKeyCombinationPressed(object? sender, KeyCombinationEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

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
        if (e.Handled)
        {
            return;
        }

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
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        _cancellationTokenSource.Cancel();
        _keyboardHandler?.Dispose();
        _cancellationTokenSource?.Dispose();
        _aiOperationCts?.Dispose();

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
        if (_commandHistory.Count == 0)
        {
            return;
        }

        if (up)
        {
            if (_commandHistoryIndex == -1)
            {
                _commandHistoryIndex = _commandHistory.Count - 1;
                _inputContext.CurrentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex > 0)
            {
                _commandHistoryIndex--;
                _inputContext.CurrentInput = _commandHistory[_commandHistoryIndex];
            }
        }
        else
        {
            if (_commandHistoryIndex >= 0 && _commandHistoryIndex < _commandHistory.Count - 1)
            {
                _commandHistoryIndex++;
                _inputContext.CurrentInput = _commandHistory[_commandHistoryIndex];
            }
            else if (_commandHistoryIndex == _commandHistory.Count - 1)
            {
                _commandHistoryIndex = -1;
                _inputContext.CurrentInput = string.Empty;
            }
        }

        _inputContext.CursorPosition = _inputContext.CurrentInput.Length;
        UpdateAutocompleteState();
    }

    private async Task SubmitCurrentInput()
    {
        if (string.IsNullOrWhiteSpace(_inputContext.CurrentInput) || _currentState != ChatState.Input)
        {
            return;
        }

        var inputToSubmit = _inputContext.CurrentInput;
        AddToCommandHistory(inputToSubmit);
        ClearCurrentInput();

        await ProcessUserInput(inputToSubmit);
    }

    private void AddToCommandHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

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
            // Add spacing before user message
            _scrollbackTerminal.WriteStatic(new Markup(""));

            var userMessage = new ChatMessage(ChatRole.User, input);
            _historyManager.AddUserMessage(userMessage);
            _scrollbackTerminal.WriteStatic(RenderMessage(userMessage));

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

            // Create new cancellation token for this AI operation
            _aiOperationCts?.Dispose();
            _aiOperationCts = new CancellationTokenSource();
            _aiOperationStartTime = DateTime.Now;

            _currentState = ChatState.Thinking;

            var chatHistory = _historyManager.GetCurrentChatHistory();
            var responseStream = _appService.ProcessChatMessageAsync(chatHistory, _aiOperationCts.Token);

            var assistantMessage = new ChatMessage(ChatRole.Assistant, "");
            _historyManager.AddAssistantMessage(assistantMessage);
            _scrollbackTerminal.WriteStatic(new Markup(""));
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
                        _scrollbackTerminal.WriteStatic(new Markup($"[green]•[/] [dim]{_toolProgress}[/]"));
                    }

                    // Handle tool result display
                    await HandleToolExecutionResult(responseUpdate);
                }
            }
            _scrollbackTerminal.WriteStatic(new Markup(""));
            _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage));
        }
        catch (OperationCanceledException) when (_aiOperationCts?.Token.IsCancellationRequested == true)
        {
            // AI operation was cancelled by user - this is handled by InterruptAiOperation()
            _logger?.LogDebug("AI operation was cancelled by user");
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
        _logger?.LogDebug("=== IsToolExecutionUpdate START ===");
        _logger?.LogDebug("ResponseUpdate.Contents is null: {IsNull}", responseUpdate.Contents == null);
        _logger?.LogDebug("ResponseUpdate.Text: '{Text}'", responseUpdate.Text ?? "NULL");

        if (responseUpdate.Contents == null)
        {
            _logger?.LogDebug("Contents is null, checking text for tool responses");
            
            // Check for tool response XML in text content even when Contents is null
            if (!string.IsNullOrEmpty(responseUpdate.Text))
            {
                var hasToolResponseStart = responseUpdate.Text.Contains("<tool_response");
                var hasToolResponseEnd = responseUpdate.Text.Contains("</tool_response>");
                _logger?.LogDebug("Text contains '<tool_response': {HasStart}", hasToolResponseStart);
                _logger?.LogDebug("Text contains '</tool_response>': {HasEnd}", hasToolResponseEnd);

                var hasToolResponse = hasToolResponseStart || hasToolResponseEnd;
                if (hasToolResponse)
                {
                    _logger?.LogDebug("=== IsToolExecutionUpdate END: TRUE (XML Content in Text) ===");
                    return true;
                }
            }
            
            _logger?.LogDebug("=== IsToolExecutionUpdate END: FALSE (No Contents, No XML) ===");
            return false;
        }

        _logger?.LogDebug("Contents count: {Count}", responseUpdate.Contents.Count());
        foreach (var content in responseUpdate.Contents)
        {
            _logger?.LogDebug("Content type: {Type}", content.GetType().Name);
        }

        // Check for function call/result content
        var hasFunctionContent = responseUpdate.Contents.Any(content => content is FunctionCallContent or FunctionResultContent);
        _logger?.LogDebug("Has function call/result content: {HasFunction}", hasFunctionContent);

        if (hasFunctionContent)
        {
            _logger?.LogDebug("=== IsToolExecutionUpdate END: TRUE (Function Content) ===");
            return true;
        }

        // Check for tool response XML in text content
        if (!string.IsNullOrEmpty(responseUpdate.Text))
        {
            var hasToolResponseStart = responseUpdate.Text.Contains("<tool_response");
            var hasToolResponseEnd = responseUpdate.Text.Contains("</tool_response>");
            _logger?.LogDebug("Text contains '<tool_response': {HasStart}", hasToolResponseStart);
            _logger?.LogDebug("Text contains '</tool_response>': {HasEnd}", hasToolResponseEnd);

            var hasToolResponse = hasToolResponseStart || hasToolResponseEnd;
            if (hasToolResponse)
            {
                _logger?.LogDebug("=== IsToolExecutionUpdate END: TRUE (XML Content) ===");
                return true;
            }
        }

        _logger?.LogDebug("=== IsToolExecutionUpdate END: FALSE ===");
        return false;
    }

    private void ExtractToolNameFromUpdate(ChatResponseUpdate responseUpdate)
    {
        if (responseUpdate.Contents == null)
        {
            return;
        }

        var functionCall = responseUpdate.Contents.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall != null)
        {
            var toolName = functionCall.Name ?? "Unknown Tool";

            // Create concise tool display with arrow format: toolName → keyValue
            if (functionCall.Arguments != null && functionCall.Arguments.Count > 0)
            {
                var keyValue = GetKeyArgumentValue(toolName, functionCall.Arguments);
                _currentToolName = !string.IsNullOrEmpty(keyValue) ? $"{toolName} → {keyValue}" : toolName;
            }
            else
            {
                _currentToolName = toolName;
            }
        }
        else
        {
            _currentToolName = string.Empty;
        }
    }

    private string GetKeyArgumentValue(string toolName, IDictionary<string, object?> arguments)
    {
        // Define key arguments for common tools to show the most relevant info
        var keyArguments = toolName.ToLowerInvariant() switch
        {
            "execute_command" or "shell" => new[] { "command", "cmd" },
            "write_file" or "writefile" or "write_to_file" => ["file_path", "path", "filename"],
            "read_file" or "readfile" or "read_file_tool" => ["file_path", "path", "filename"],
            "edit_file" or "editfile" => ["file_path", "path", "filename"],
            "grep" or "search" => ["pattern", "query", "search_term"],
            "ls" or "list" or "list_files" => ["path", "directory"],
            _ => ["path", "file", "command", "query", "name"] // fallback common keys
        };

        foreach (var key in keyArguments)
        {
            if (arguments.TryGetValue(key, out var value) && value != null)
            {
                var valueStr = value.ToString() ?? "";

                // For file paths, show just the filename if it's a full path
                if (key.Contains("path") || key.Contains("file"))
                {
                    valueStr = Path.GetFileName(valueStr);
                    if (string.IsNullOrEmpty(valueStr))
                    {
                        // If GetFileName returns empty, use the original but truncate
                        valueStr = value.ToString() ?? "";
                    }
                }

                // For commands, show first part (command name) and truncate if needed
                if (key.Contains("command") || key.Contains("cmd"))
                {
                    var parts = valueStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        valueStr = parts[0];
                        if (parts.Length > 1)
                        {
                            // Add indication there are more arguments
                            valueStr += "...";
                        }
                    }
                }

                // Final truncation to ensure single line (max 25 chars for the value part)
                if (valueStr.Length > 25)
                {
                    valueStr = valueStr[..22] + "...";
                }

                return valueStr;
            }
        }

        // If no key argument found, return empty to show just tool name
        return string.Empty;
    }

    private void InsertCharacter(char character)
    {
        if (_currentState != ChatState.Input)
        {
            return;
        }

        _inputContext.CursorPosition = Math.Max(0, Math.Min(_inputContext.CursorPosition, _inputContext.CurrentInput.Length));
        _inputContext.CurrentInput = _inputContext.CurrentInput.Insert(_inputContext.CursorPosition, character.ToString());
        _inputContext.CursorPosition++;
        _commandHistoryIndex = -1;
        UpdateAutocompleteState();
    }

    private void DeleteCharacterBefore()
    {
        if (_currentState != ChatState.Input || _inputContext.CurrentInput.Length == 0)
        {
            return;
        }

        _inputContext.CursorPosition = Math.Max(0, Math.Min(_inputContext.CursorPosition, _inputContext.CurrentInput.Length));

        if (_inputContext.CursorPosition > 0)
        {
            _inputContext.CurrentInput = _inputContext.CurrentInput.Remove(_inputContext.CursorPosition - 1, 1);
            _inputContext.CursorPosition--;
        }

        _commandHistoryIndex = -1;
        UpdateAutocompleteState();
    }

    private void DeleteCharacterAfter()
    {
        if (_currentState != ChatState.Input || _inputContext.CurrentInput.Length == 0)
        {
            return;
        }

        _inputContext.CursorPosition = Math.Max(0, Math.Min(_inputContext.CursorPosition, _inputContext.CurrentInput.Length));

        if (_inputContext.CursorPosition < _inputContext.CurrentInput.Length)
        {
            _inputContext.CurrentInput = _inputContext.CurrentInput.Remove(_inputContext.CursorPosition, 1);
        }

        _commandHistoryIndex = -1;
        UpdateAutocompleteState();
    }

    private void MoveCursorLeft()
    {
        if (_inputContext.CursorPosition > 0)
        {
            _inputContext.CursorPosition--;
            UpdateAutocompleteState();
        }
    }

    private void MoveCursorRight()
    {
        if (_inputContext.CursorPosition < _inputContext.CurrentInput.Length)
        {
            _inputContext.CursorPosition++;
            UpdateAutocompleteState();
        }
    }

    private void MoveCursorToStart()
    {
        _inputContext.CursorPosition = 0;
        UpdateAutocompleteState();
    }

    private void MoveCursorToEnd()
    {
        _inputContext.CursorPosition = _inputContext.CurrentInput.Length;
        UpdateAutocompleteState();
    }

    private void ClearCurrentInput()
    {
        _inputContext.Clear();
        _commandHistoryIndex = -1;
    }

    private async void UpdateAutocompleteState()
    {
        try
        {
            // Detect which provider should be triggered
            var provider = _autocompleteManager.DetectTrigger(_inputContext.CurrentInput, _inputContext.CursorPosition);

            if (provider != null)
            {
                _inputContext.ActiveProvider = provider;
                await _autocompleteManager.UpdateSuggestionsAsync(_inputContext);
            }
            else
            {
                _inputContext.ClearAutocomplete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating autocomplete state");
            _inputContext.ClearAutocomplete();
        }
    }

    private void NavigateAutocomplete(bool up)
    {
        if (!_inputContext.ShowSuggestions || _inputContext.Suggestions.Count == 0)
        {
            return;
        }

        if (up)
        {
            _inputContext.SelectedSuggestionIndex =
                (_inputContext.SelectedSuggestionIndex - 1 + _inputContext.Suggestions.Count)
                % _inputContext.Suggestions.Count;
        }
        else
        {
            _inputContext.SelectedSuggestionIndex =
                (_inputContext.SelectedSuggestionIndex + 1)
                % _inputContext.Suggestions.Count;
        }
    }

    private void AcceptAutocompleteSuggestion()
    {
        _autocompleteManager.AcceptSuggestion(_inputContext);
    }

    private void CancelAutocomplete()
    {
        _inputContext.ClearAutocomplete();
    }

    private void HandleEscapeKey()
    {
#pragma warning disable IDE0010 // Add missing cases
        switch (_currentState)
        {
            case ChatState.Input:
                // Clear current input when in input state
                ClearCurrentInput();
                break;

            case ChatState.Thinking:
            case ChatState.ToolExecution:
                // Interrupt AI operation and return to input
                InterruptAiOperation();
                break;
        }
#pragma warning restore IDE0010 // Add missing cases
    }

    private void InterruptAiOperation()
    {
        try
        {
            // Cancel the AI operation
            _aiOperationCts?.Cancel();

            // Set state back to input
            _currentState = ChatState.Input;

            // Clear any progress indicators
            _toolProgress = string.Empty;
            _currentToolName = string.Empty;

            // Display interruption message without assistant prefix
            _scrollbackTerminal.WriteStatic(new Markup("⚠ Request cancelled."));
            _scrollbackTerminal.WriteStatic(new Markup(""));

            // Add to history as assistant message for context
            var interruptMessage = new ChatMessage(ChatRole.Assistant, "⚠ Request cancelled.");
            _historyManager.AddAssistantMessage(interruptMessage);

            _logger?.LogInformation("AI operation interrupted by user (Escape key)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error interrupting AI operation");
        }
    }

    private async Task HandleToolExecutionResult(ChatResponseUpdate responseUpdate)
    {
        // Extract call ID early for consistent logging prefix
        var callId = ExtractCallIdFromUpdate(responseUpdate);
        var logPrefix = !string.IsNullOrEmpty(callId) ? $"[{callId}] " : "";

        _logger?.LogDebug("{Prefix}=== HandleToolExecutionResult START ===", logPrefix);

        try
        {
            _logger?.LogDebug("{Prefix}Contents is null: {IsNull}", logPrefix, responseUpdate.Contents == null);
            _logger?.LogDebug("{Prefix}Text is null or empty: {IsEmpty}", logPrefix, string.IsNullOrEmpty(responseUpdate.Text));

            if (responseUpdate.Contents == null && string.IsNullOrEmpty(responseUpdate.Text))
            {
                _logger?.LogDebug("{Prefix}Both Contents and Text are null/empty, returning early", logPrefix);
                _logger?.LogDebug("{Prefix}=== HandleToolExecutionResult END: EARLY RETURN ===", logPrefix);
                return;
            }

            string? toolName = null;
            string? result = null;

            // Handle function call content (tool execution starting)
            var functionCall = responseUpdate.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
            if (functionCall != null)
            {
                // Store the mapping of call ID to tool name for later use
                _functionCallToToolName[functionCall.CallId] = functionCall.Name;
                _logger?.LogDebug("{Prefix}Function call stored - CallId: '{CallId}', ToolName: '{ToolName}'", 
                    logPrefix, functionCall.CallId, functionCall.Name);

                // For EditTool, capture the pre-edit content (Gemini CLI approach)
                await CapturePreEditContentForEditTool(functionCall);
            }

            // Handle function result content (tool execution completed)
            var functionResult = responseUpdate.Contents?.OfType<FunctionResultContent>().FirstOrDefault();
            _logger?.LogDebug("{Prefix}FunctionResult found: {Found}", logPrefix, functionResult != null);

            if (functionResult != null)
            {
                // Get the tool name from our stored mapping
                _ = _functionCallToToolName.TryGetValue(functionResult.CallId, out toolName);
                toolName ??= "Unknown Tool";

                result = functionResult.Result?.ToString() ?? "";
                _logger?.LogDebug("{Prefix}Function result - CallId: '{CallId}', ToolName: '{ToolName}', Result length: {Length}", 
                    logPrefix, functionResult.CallId, toolName, result.Length);
            }
            // If we only have a function call but no result yet, don't process for display
            else if (functionCall != null && functionResult == null)
            {
                _logger?.LogDebug("{Prefix}Function call detected but no result yet - skipping display processing", logPrefix);
                _logger?.LogDebug("{Prefix}=== HandleToolExecutionResult END: FUNCTION CALL ONLY ===", logPrefix);
                return;
            }
            // Handle XML tool responses in text content
            else if (!string.IsNullOrEmpty(responseUpdate.Text) && 
                     (responseUpdate.Text.Contains("<tool_response") || responseUpdate.Text.Contains("</tool_response>")))
            {
                result = responseUpdate.Text;
                _logger?.LogDebug("{Prefix}XML tool response found in text, length: {Length}", logPrefix, result.Length);

                // Extract tool name from XML if possible
                try
                {
                    var toolNameMatch = System.Text.RegularExpressions.Regex.Match(result, @"tool_name=""([^""]+)""");
                    toolName = toolNameMatch.Success ? toolNameMatch.Groups[1].Value : "Unknown Tool";
                    _logger?.LogDebug("{Prefix}Extracted tool name from XML: '{ToolName}'", logPrefix, toolName);
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "{Prefix}Failed to extract tool name from XML", logPrefix);
                    toolName = "Unknown Tool";
                }
            }
            else
            {
                _logger?.LogDebug("{Prefix}No function result or XML tool response found", logPrefix);
            }

            _logger?.LogDebug("{Prefix}Final values - ToolName: '{ToolName}', Result is null/empty: {IsEmpty}", 
                logPrefix, toolName, string.IsNullOrEmpty(result));

            // If we have a tool result to display
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(toolName))
            {
                _logger?.LogDebug("{Prefix}Processing tool result for display...", logPrefix);

                // Parse the tool response for enhanced display
                var toolInfo = _toolResponseParser.ParseToolResponse(toolName, result);
                _logger?.LogDebug("{Prefix}Parsed tool info - Status: {Status}, FilePath: '{FilePath}', HasNewContent: {HasContent}", 
                    logPrefix, toolInfo.Status, toolInfo.FilePath, !string.IsNullOrEmpty(toolInfo.NewContent));

                // Handle different tool types appropriately
                UnifiedDiff? diff = null;
                string? displayContent = null;

                try
                {
                    var normalizedToolName = toolInfo.ToolName.ToLowerInvariant();

                    // For WriteFileTool - show content directly, no diff
                    if (IsWriteFileTool(normalizedToolName))
                    {
                        _logger?.LogDebug("{Prefix}WriteFileTool detected - using content for display", logPrefix);
                        displayContent = toolInfo.NewContent ?? ExtractContentFromXml(result);
                    }
                    // For EditTool and DiffPatchTools - generate/extract diffs
                    else if (IsEditTool(normalizedToolName) || IsDiffPatchTool(normalizedToolName))
                    {
                        _logger?.LogDebug("{Prefix}Edit/DiffPatch tool detected - generating diff", logPrefix);

                        if (!string.IsNullOrEmpty(toolInfo.FilePath))
                        {
                            _logger?.LogDebug("{Prefix}FilePath found - proceeding with diff generation", logPrefix);

                            string? originalContent = null;
                            var newContent = toolInfo.NewContent;

                            // For EditTool, use the captured pre-edit content (Gemini CLI approach)
                            if (IsEditTool(normalizedToolName) && functionResult != null)
                            {
                                _logger?.LogDebug("{Prefix}EditTool detected - using captured pre-edit content", logPrefix);
                                if (_functionCallToPreEditContent.TryGetValue(functionResult.CallId, out var preEditContent))
                                {
                                    originalContent = preEditContent;
                                    _logger?.LogDebug("{Prefix}Retrieved pre-edit content, length: {Length}", logPrefix, originalContent.Length);
                                }
                                else
                                {
                                    _logger?.LogDebug("{Prefix}No pre-edit content found for CallId: {CallId}", logPrefix, functionResult.CallId);
                                }
                            }
                            else
                            {
                                // For DiffPatchTools, try to read original content from file
                                if (File.Exists(toolInfo.FilePath))
                                {
                                    try
                                    {
                                        originalContent = await File.ReadAllTextAsync(toolInfo.FilePath);
                                        _logger?.LogDebug("{Prefix}Read original content for diff, length: {Length}", logPrefix, originalContent.Length);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogDebug(ex, "{Prefix}Could not read original file content for diff: {FilePath}", logPrefix, toolInfo.FilePath);
                                    }
                                }
                            }

                            // For EditTool, use content_on_disk as new content
                            // For DiffPatchTools, read current file content if needed
                            if (string.IsNullOrEmpty(newContent) && File.Exists(toolInfo.FilePath))
                            {
                                try
                                {
                                    newContent = await File.ReadAllTextAsync(toolInfo.FilePath);
                                    _logger?.LogDebug("{Prefix}Read current file content as new content, length: {Length}", logPrefix, newContent.Length);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogDebug(ex, "{Prefix}Could not read current file content: {FilePath}", logPrefix, toolInfo.FilePath);
                                }
                            }

                        // Set up logger for UnifiedDiffGenerator debugging
                        UnifiedDiffGenerator.SetLogger(_logger);

                        // Generate diff using the tool response parser
                        diff = _toolResponseParser.ExtractFileDiff(
                            toolInfo.ToolName,
                            result,
                            originalContent,
                            newContent,
                            toolInfo.FilePath);

                        _logger?.LogDebug("{Prefix}Generated diff: {HasDiff}", logPrefix, diff != null);
                        if (diff != null)
                        {
                            _logger?.LogDebug("{Prefix}Diff details - OriginalFile: '{OriginalFile}', ModifiedFile: '{ModifiedFile}', HunkCount: {HunkCount}", 
                                logPrefix, diff.OriginalFile, diff.ModifiedFile, diff.Hunks?.Count ?? 0);
                            
                            if (diff.Hunks?.Count > 0)
                            {
                                var firstHunk = diff.Hunks[0];
                                _logger?.LogDebug("{Prefix}First hunk - OriginalStart: {OriginalStart}, ModifiedStart: {ModifiedStart}, LineCount: {LineCount}", 
                                    logPrefix, firstHunk.OriginalStart, firstHunk.ModifiedStart, firstHunk.Lines?.Count ?? 0);
                                
                                if (firstHunk.Lines?.Count > 0)
                                {
                                    var sampleLines = firstHunk.Lines.Take(3).Select(l => $"{l.Type}: {l.Content}").ToArray();
                                    _logger?.LogDebug("{Prefix}Sample diff lines: {SampleLines}", logPrefix, string.Join(" | ", sampleLines));
                                }
                            }
                        }
                        }
                        else
                        {
                            _logger?.LogDebug("{Prefix}Skipping diff generation - FilePath is empty", logPrefix);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug("{Prefix}Unknown tool type - using summary for display", logPrefix);
                        displayContent = toolInfo.Summary;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "{Prefix}Could not process tool result for display", logPrefix);
                }

                _logger?.LogDebug("{Prefix}Creating tool display...", logPrefix);

                // Create enhanced tool display with clean styling
                var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                    toolInfo.ToolName,
                    toolInfo.Status,
                    toolInfo.Description,
                    diff: diff,
                    result: displayContent ?? toolInfo.Summary ?? result
                );

                _logger?.LogDebug("{Prefix}Displaying tool result in scrollback...", logPrefix);

                // Display the tool execution result in scrollback
                _scrollbackTerminal.WriteStatic(toolDisplay);
                _scrollbackTerminal.WriteStatic(new Markup(""));

                _logger?.LogDebug("{Prefix}Tool result displayed successfully", logPrefix);
            }
            else
            {
                _logger?.LogDebug("{Prefix}Skipping tool display - missing toolName or result", logPrefix);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "{Prefix}Error handling tool execution result", logPrefix);
        }

        _logger?.LogDebug("{Prefix}=== HandleToolExecutionResult END ===", logPrefix);
        await Task.CompletedTask;
    }

    private static bool IsWriteFileTool(string normalizedToolName)
    {
        return normalizedToolName is "write_file" or "writefile" or "write_to_file";
    }

    private static bool IsEditTool(string normalizedToolName)
    {
        return normalizedToolName is "replace" or "edit_file" or "editfile" or "edit";
    }

    private static bool IsDiffPatchTool(string normalizedToolName)
    {
        return normalizedToolName is "apply_code_patch" or "generate_code_patch" or "preview_patch_application";
    }

    private static string? ExtractContentFromXml(string xmlResponse)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlResponse);

            // Look for content_on_disk element
            var contentNode = doc.SelectSingleNode("//content_on_disk");
            if (contentNode != null)
            {
                return contentNode.InnerText;
            }

            // Fallback: look for any content in notes
            var notesNode = doc.SelectSingleNode("//notes");
            if (notesNode != null)
            {
                return notesNode.InnerText?.Trim();
            }

            return null;
        }
        catch (XmlException)
        {
            // If XML parsing fails, return null
            return null;
        }
    }

    private async Task CapturePreEditContentForEditTool(FunctionCallContent functionCall)
    {
        _logger?.LogDebug("=== CapturePreEditContentForEditTool START ===");

        try
        {
            // Only capture for EditTool
            if (!IsEditTool(functionCall.Name.ToLowerInvariant()))
            {
                _logger?.LogDebug("Not an EditTool, skipping pre-edit capture");
                return;
            }

            // Extract file_path from function arguments
            if (functionCall.Arguments?.TryGetValue("file_path", out var filePathValue) == true)
            {
                var filePath = filePathValue?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    _logger?.LogDebug("Capturing pre-edit content for file: {FilePath}", filePath);

                    // Read the entire file content before the edit (Gemini CLI approach)
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var preEditContent = await File.ReadAllTextAsync(filePath);
                            _functionCallToPreEditContent[functionCall.CallId] = preEditContent;
                            _logger?.LogDebug("Captured pre-edit content, length: {Length}", preEditContent.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogDebug(ex, "Failed to read pre-edit content from file: {FilePath}", filePath);
                        }
                    }
                    else
                    {
                        _logger?.LogDebug("File does not exist, storing empty pre-edit content: {FilePath}", filePath);
                        _functionCallToPreEditContent[functionCall.CallId] = string.Empty;
                    }
                }
                else
                {
                    _logger?.LogDebug("File path is null or empty");
                }
            }
            else
            {
                _logger?.LogDebug("No file_path argument found in function call");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error capturing pre-edit content");
        }

        _logger?.LogDebug("=== CapturePreEditContentForEditTool END ===");
    }

    private string? ExtractOriginalContentFromFunctionCall(FunctionCallContent? functionCall)
    {
        if (functionCall?.Arguments == null)
        {
            _logger?.LogDebug("No function call or arguments available for original content extraction");
            return null;
        }

        // For EditTool (replace), extract the old_string parameter
        if (functionCall.Arguments.TryGetValue("old_string", out var oldStringValue))
        {
            var oldString = oldStringValue?.ToString();
            _logger?.LogDebug("Extracted old_string from function call, length: {Length}", oldString?.Length ?? 0);
            return oldString;
        }

        _logger?.LogDebug("No old_string parameter found in function call arguments");
        return null;
    }

    private string? ExtractCallIdFromUpdate(ChatResponseUpdate responseUpdate)
    {
        // Try to extract call ID from function call content
        var functionCall = responseUpdate.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
        if (functionCall != null)
        {
            return functionCall.CallId;
        }

        // Try to extract call ID from function result content
        var functionResult = responseUpdate.Contents?.OfType<FunctionResultContent>().FirstOrDefault();
        if (functionResult != null)
        {
            return functionResult.CallId;
        }

        // No call ID found
        return null;
    }
}
