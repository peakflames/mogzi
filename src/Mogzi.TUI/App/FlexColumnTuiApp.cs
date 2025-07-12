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
        _slashCommandProcessor.SessionClearRequested += OnSessionClearRequested;
        _slashCommandProcessor.SessionRenameRequested += OnSessionRenameRequested;

        RegisterKeyBindings();

    }

    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_logger is null)
        {
            Console.WriteLine("Logger is null");
            return 7;
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
            return 8;
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
                var sessionLoaded = false;

                // First try to load by GUID (existing behavior)
                if (Guid.TryParse(sessionId, out _))
                {
                    try
                    {
                        await sessionManager.LoadSessionAsync(sessionId);
                        sessionLoaded = true;
                        _logger.LogInformation("Loaded session by ID: {SessionId}", sessionId);
                    }
                    catch (FileNotFoundException)
                    {
                        _logger.LogWarning("Session not found by ID: {SessionId}", sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load session by ID: {SessionId}", sessionId);
                    }
                }

                // If GUID loading failed or sessionId is not a GUID, try loading by name
                if (!sessionLoaded)
                {
                    try
                    {
                        sessionLoaded = await sessionManager.TryLoadSessionByNameAsync(sessionId);
                        if (sessionLoaded)
                        {
                            _logger.LogInformation("Loaded session by name: {SessionName}", sessionId);
                        }
                        else
                        {
                            _logger.LogWarning("No session found with name: {SessionName}", sessionId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load session by name: {SessionName}", sessionId);
                    }
                }

                // If neither GUID nor name loading succeeded, create new session
                if (!sessionLoaded)
                {
                    _logger.LogInformation("Creating new session since no existing session was found for: {SessionIdentifier}", sessionId);
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
                else if (message.Role == ChatRole.Tool)
                {
                    _historyManager.AddToolMessageWithoutPersistence(message);
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

            // Handle auto-submit for piped input
            if (_tuiContext.AutoSubmitPipedInput && !string.IsNullOrEmpty(_tuiContext.InputContext.CurrentInput))
            {
                _logger.LogInformation("Auto-submitting piped input: {Input}", _tuiContext.InputContext.CurrentInput);

                // Schedule auto-submit to happen after the UI is fully initialized
                _ = Task.Run(async () =>
                {
                    // Small delay to ensure UI is ready
                    await Task.Delay(100);

                    // Simulate Enter key press to submit the piped input
                    var enterKeyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
                    var enterKeyArgs = new KeyPressEventArgs(enterKeyInfo);
                    await _stateManager.HandleKeyPressAsync(enterKeyArgs);

                    // Refresh the display
                    _scrollbackTerminal.Refresh();
                });
            }
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
        // Always render welcome screen first, regardless of whether we have chat history
        // This ensures the welcome message is displayed at startup for both new and loaded sessions
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

        // If we have existing chat history, load it after the welcome message
        var chatHistory = _historyManager.GetCurrentChatHistory();
        if (chatHistory.Any())
        {
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
            return 9;
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
        var components = new List<IRenderable>();

        // Handle text content
        if (!string.IsNullOrEmpty(message.Text))
        {
            var messageType = GetMessageType(message);
            var prefix = messageType switch
            {
                MessageType.User => "[dim]>[/] ",
                MessageType.Assistant => "✦ ",
                MessageType.Tool => "◉ ",
                _ => ""
            };
            var color = messageType switch
            {
                MessageType.User => "dim",
                MessageType.Assistant => "skyblue1",
                MessageType.Tool => "orange1",
                _ => "white"
            };

            // Strip system environment context from user messages for display
            var displayText = message.Role == ChatRole.User
                ? Mogzi.Utils.MessageUtils.StripSystemEnvironment(message.Text)
                : message.Text;

            components.Add(new Markup($"[{color}]{prefix}{displayText}[/]"));
        }

        // Handle function calls and results for tool execution display
        if (message.Contents != null && message.Contents.Count > 0)
        {
            // Check if this message contains function results (Tool role messages)
            var functionResults = message.Contents.OfType<FunctionResultContent>().ToList();
            var functionCalls = message.Contents.OfType<FunctionCallContent>().ToList();

            if (functionResults.Count > 0)
            {
                // This is a Tool message with function results - create comprehensive tool displays
                foreach (var functionResult in functionResults)
                {
                    var toolResponseParser = _serviceProvider.GetRequiredService<ToolResponseParser>();
                    var result = functionResult.Result?.ToString() ?? "";

                    // Try to get tool name from the result
                    var toolName = ExtractToolNameFromResult(result) ?? "Tool";
                    var toolInfo = toolResponseParser.ParseToolResponse(toolName, result);

                    // Create comprehensive tool display with result
                    var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                        toolInfo.ToolName,
                        toolInfo.Status,
                        toolInfo.Description,
                        diff: null,
                        result: toolInfo.Summary ?? result
                    );
                    components.Add(toolDisplay);
                }
            }
            else if (functionCalls.Count > 0)
            {
                // This is an Assistant message with function calls - create simple tool displays
                foreach (var functionCall in functionCalls)
                {
                    // Create tool display for function call (without result)
                    var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                        functionCall.Name ?? "Unknown Tool",
                        ToolExecutionStatus.Success,
                        GetToolDescription(functionCall),
                        diff: null,
                        result: null
                    );
                    components.Add(toolDisplay);
                }
            }
        }

        // Return appropriate renderable based on what we have
        if (components.Count == 0)
        {
            return new Text(string.Empty);
        }
        else if (components.Count == 1)
        {
            return components[0];
        }
        else
        {
            return new Rows(components);
        }
    }

    /// <summary>
    /// Gets a description for a function call based on its arguments.
    /// </summary>
    private static string? GetToolDescription(FunctionCallContent functionCall)
    {
        if (functionCall.Arguments == null || functionCall.Arguments.Count == 0)
        {
            return null;
        }

        // Extract key argument for display
        var toolName = functionCall.Name?.ToLowerInvariant() ?? "";
        var keyArguments = toolName switch
        {
            "read_text_file" or "read_file" => new[] { "absolute_path", "file_path", "path" },
            "write_file" or "write_to_file" => ["file_path", "path"],
            "execute_command" or "shell" => ["command"],
            "list_directory" or "ls" => ["path", "directory"],
            _ => ["path", "file_path", "command", "query"]
        };

        foreach (var key in keyArguments)
        {
            if (functionCall.Arguments.TryGetValue(key, out var value) && value != null)
            {
                var valueStr = value.ToString() ?? "";

                // For file paths, show just the filename
                if (key.Contains("path") || key.Contains("file"))
                {
                    valueStr = Path.GetFileName(valueStr);
                    if (string.IsNullOrEmpty(valueStr))
                    {
                        valueStr = value.ToString() ?? "";
                    }
                }

                // Truncate if too long
                if (valueStr.Length > 50)
                {
                    valueStr = valueStr[..47] + "...";
                }

                return valueStr;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts tool name from XML tool result.
    /// </summary>
    private static string? ExtractToolNameFromResult(string result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        try
        {
            var toolNameMatch = System.Text.RegularExpressions.Regex.Match(result, @"tool_name=""([^""]+)""");
            return toolNameMatch.Success ? toolNameMatch.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
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
        else if (message.Role == ChatRole.Tool)
        {
            return MessageType.Tool;
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

    /// <summary>
    /// Handles session clear requests from slash commands.
    /// </summary>
    private async void OnSessionClearRequested()
    {
        try
        {
            var sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
            await sessionManager.ClearCurrentSessionAsync();

            // Clear the UI history as well, but don't re-initialize the scrollback immediately
            // This allows the feedback message to be displayed
            _historyManager.ClearHistory();

            _logger.LogInformation("Session history cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing session history");
        }
    }

    /// <summary>
    /// Handles session rename requests from slash commands.
    /// </summary>
    private async void OnSessionRenameRequested(string newName)
    {
        try
        {
            var sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
            await sessionManager.RenameSessionAsync(newName);

            _logger.LogInformation("Session renamed to: {SessionName}", newName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renaming session to: {SessionName}", newName);
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
