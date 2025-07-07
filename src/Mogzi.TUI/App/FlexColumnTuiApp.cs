namespace Mogzi.TUI.App;

public sealed class FlexColumnTuiApp : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlexColumnTuiApp> _logger;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiComponentManager _componentManager;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly IScrollbackTerminal _scrollbackTerminal;
    private readonly HistoryManager _historyManager;
    private readonly SlashCommandProcessor _slashCommandProcessor;
    private bool _isDisposed = false;

    public bool IsRunning { get; private set; } = false;

    public event Action? Started;
    public event Action? Stopped;
    public event Action<Exception>? UnhandledError;

    public FlexColumnTuiApp(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<FlexColumnTuiApp>>();
        _stateManager = serviceProvider.GetRequiredService<ITuiStateManager>();
        _tuiContext = serviceProvider.GetRequiredService<ITuiContext>();
        _componentManager = serviceProvider.GetRequiredService<ITuiComponentManager>();
        _scrollbackTerminal = serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _historyManager = serviceProvider.GetRequiredService<HistoryManager>();
        _slashCommandProcessor = serviceProvider.GetRequiredService<SlashCommandProcessor>();

        _cancellationTokenSource = new CancellationTokenSource();

        // Get the keyboard handler from DI container instead of creating a new instance
        _keyboardHandler = serviceProvider.GetRequiredService<AdvancedKeyboardHandler>();

        _keyboardHandler.KeyPressed += OnKeyPressed;
        _keyboardHandler.KeyCombinationPressed += OnKeyCombinationPressed;
        _keyboardHandler.CharacterTyped += OnCharacterTyped;

        // Wire up slash command processor events
        _slashCommandProcessor.ExitRequested += () => _cancellationTokenSource.Cancel();
        _slashCommandProcessor.ClearHistoryRequested += () =>
        {
            _historyManager.ClearHistory();
            _scrollbackTerminal.Initialize();
            RenderInitialContent();
        };
        _slashCommandProcessor.InteractiveCommandRequested += OnInteractiveCommandRequested;

        RegisterKeyBindings();

    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_logger is null)
        {
            Console.WriteLine("Logger is null");
            return 1;
        }

        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Application is already running");
        }

        try
        {
            _logger.LogInformation("Starting FlexColumn TUI application");

            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

            await Initialize(args);

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

    private async Task Initialize(string[] args)
    {
        try
        {
            // Parse arguments for session management
            var parsedArgs = ArgumentParser.Parse(args);
            var sessionId = ArgumentParser.GetString(parsedArgs, ["session", "s"], null);

            // Initialize session management
            var sessionManager = _serviceProvider.GetRequiredService<SessionManager>();

            if (!string.IsNullOrEmpty(sessionId))
            {
                try
                {
                    await sessionManager.LoadSessionAsync(sessionId);
                    _logger.LogInformation("Loaded session: {SessionId}", sessionId);
                }
                catch (FileNotFoundException)
                {
                    _logger.LogWarning("Session not found: {SessionId}, creating new session", sessionId);
                    await sessionManager.CreateNewSessionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load session: {SessionId}, creating new session", sessionId);
                    await sessionManager.CreateNewSessionAsync();
                }
            }
            else
            {
                // Create new session if no session ID provided
                await sessionManager.CreateNewSessionAsync();
            }

            // Set the session manager on history manager for persistence
            _historyManager.SetSessionManager(sessionManager);

            // Load session messages into history manager for UI display
            var sessionMessages = sessionManager.GetCurrentSessionMessages();
            foreach (var message in sessionMessages)
            {
                // Add messages according to their actual role, but skip persistence since they're already persisted
                if (message.Role == ChatRole.User)
                {
                    _historyManager.AddUserMessageWithoutPersistence(message);
                }
                else if (message.Role == ChatRole.Assistant)
                {
                    _historyManager.AddAssistantMessageWithoutPersistence(message);
                }
                // Note: We could add support for other roles if needed
            }

            // Register state factories
            RegisterStateFactories();

            // Initialize state manager with context
            if (_tuiContext is null)
            {
                throw new InvalidOperationException("TuiContext is null during initialization");
            }

            if (_stateManager is null)
            {
                throw new InvalidOperationException("StateManager is null during initialization");
            }

            await _stateManager.InitializeAsync(_tuiContext);

            // Initialize the terminal and render initial content
            _scrollbackTerminal.Initialize();
            RenderInitialContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize FlexColumn TUI application");
            throw;
        }
    }

    private void RegisterStateFactories()
    {
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
    }

    private void RenderInitialContent()
    {
        var chatHistory = _historyManager.GetCurrentChatHistory();
        if (!chatHistory.Any())
        {
            // Render welcome screen to static area to prevent it from being overwritten by dynamic content
            // This ensures the welcome message persists in the scrollback history
            var welcomePanel = _serviceProvider.GetRequiredService<WelcomePanel>();
            var renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();
            var themeInfo = _serviceProvider.GetRequiredService<IThemeInfo>();
            var renderContext = new RenderContext(
                _tuiContext,
                _stateManager.CurrentStateType,
                _logger,
                _serviceProvider,
                renderingUtilities,
                themeInfo
            );

            var welcomeContent = welcomePanel.Render(renderContext);
            _scrollbackTerminal.WriteStatic(welcomeContent);
            _scrollbackTerminal.WriteStatic(new Markup(""));

        }
        else
        {
            // Load existing chat history into static scrollback area
            _scrollbackTerminal.WriteStatic(new Markup($"[dim]Loading {chatHistory.Count} messages from existing chat history[/]"));
            _scrollbackTerminal.WriteStatic(new Markup(""));
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
        try
        {
            // Create render context for components
            var renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();
            var themeInfo = _serviceProvider.GetRequiredService<IThemeInfo>();
            var renderContext = new RenderContext(
                _tuiContext,
                _stateManager.CurrentStateType,
                _logger,
                _serviceProvider,
                renderingUtilities,
                themeInfo
            );

            // Update component visibility based on current state
            _componentManager.UpdateComponentVisibility(_stateManager.CurrentStateType, renderContext);

            // Render the layout using the component manager
            var result = _componentManager.RenderLayout(renderContext);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering dynamic content through component manager");
            return new Text($"Rendering error: {ex.Message}");
        }
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
            MessageType.Assistant => "skyblue1",
            _ => "white"
        };

        // Strip system environment context from user messages for display
        var displayText = message.Role == ChatRole.User
            ? Mogzi.Utils.MessageUtils.StripSystemEnvironment(message.Text)
            : message.Text;

        return new Markup($"[{color}]{prefix}{displayText}[/]");
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
            args.Handled = true;
        });

    }

    /// <summary>
    /// Handles interactive slash commands like /tool-approvals.
    /// </summary>
    private async void OnInteractiveCommandRequested(string command)
    {
        try
        {
            // Activate the user selection manager for the command
            _tuiContext.UserSelectionManager.DetectAndActivate(command);

            if (_tuiContext.UserSelectionManager.IsSelectionModeActive)
            {
                await _tuiContext.UserSelectionManager.UpdateSelectionsAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling interactive command: {Command}", command);
        }
    }

    private async void OnKeyPressed(object? sender, KeyPressEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        try
        {
            await _stateManager.HandleKeyPressAsync(e);
            // Only refresh if the event was handled and might have changed the UI state
            if (e.Handled)
            {
                _scrollbackTerminal.Refresh();
            }
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
            if (_stateManager is not null)
            {
                await _stateManager.HandleCharacterTypedAsync(e);
            }

            // Only refresh if the event was handled and might have changed the UI state
            if (e.Handled)
            {
                _scrollbackTerminal.Refresh();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character typed: {Character}", e.Character);
        }
    }

    private async Task ShutdownAsync()
    {
        try
        {
            await _keyboardHandler.StopAsync();
            _scrollbackTerminal.Shutdown();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }
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
        _tuiContext.AiOperationCts?.Dispose();

        Started = null;
        Stopped = null;
        UnhandledError = null;


        GC.SuppressFinalize(this);
    }

#pragma warning disable IDE0051 // Remove unused private members
    private void LoadCommandHistory()
#pragma warning restore IDE0051 // Remove unused private members
    {
        try
        {
            var chatHistory = _historyManager.GetCurrentChatHistory();
            var userMessages = chatHistory
                .Where(m => m.Role == ChatRole.User)
                .Select(m => m.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            _tuiContext.CommandHistory.Clear();
            _tuiContext.CommandHistory.AddRange(userMessages);

        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error loading command history");
        }
    }

    private void NavigateCommandHistory(bool up)
    {
        if (_tuiContext.CommandHistory.Count == 0)
        {
            return;
        }

        var inputContext = _tuiContext.InputContext;

        if (up)
        {
            if (_tuiContext.CommandHistoryIndex == -1)
            {
                _tuiContext.CommandHistoryIndex = _tuiContext.CommandHistory.Count - 1;
                inputContext.CurrentInput = _tuiContext.CommandHistory[_tuiContext.CommandHistoryIndex];
            }
            else if (_tuiContext.CommandHistoryIndex > 0)
            {
                _tuiContext.CommandHistoryIndex--;
                inputContext.CurrentInput = _tuiContext.CommandHistory[_tuiContext.CommandHistoryIndex];
            }
        }
        else
        {
            if (_tuiContext.CommandHistoryIndex >= 0 && _tuiContext.CommandHistoryIndex < _tuiContext.CommandHistory.Count - 1)
            {
                _tuiContext.CommandHistoryIndex++;
                inputContext.CurrentInput = _tuiContext.CommandHistory[_tuiContext.CommandHistoryIndex];
            }
            else if (_tuiContext.CommandHistoryIndex == _tuiContext.CommandHistory.Count - 1)
            {
                _tuiContext.CommandHistoryIndex = -1;
                inputContext.CurrentInput = string.Empty;
            }
        }

        inputContext.CursorPosition = inputContext.CurrentInput.Length;
    }

}
