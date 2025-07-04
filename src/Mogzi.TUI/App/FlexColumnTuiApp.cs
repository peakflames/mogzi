namespace Mogzi.TUI.App;

public sealed class FlexColumnTuiApp : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FlexColumnTuiApp> _logger;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiContext _tuiContext;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly IScrollbackTerminal _scrollbackTerminal;
    private readonly HistoryManager _historyManager;
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
        _scrollbackTerminal = serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _historyManager = serviceProvider.GetRequiredService<HistoryManager>();

        _cancellationTokenSource = new CancellationTokenSource();

        _keyboardHandler = new AdvancedKeyboardHandler(_serviceProvider.GetService<ILogger<AdvancedKeyboardHandler>>());

        _keyboardHandler.KeyPressed += OnKeyPressed;
        _keyboardHandler.KeyCombinationPressed += OnKeyCombinationPressed;
        _keyboardHandler.CharacterTyped += OnCharacterTyped;

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
    private async void Initialize(string[] args)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogDebug("Initializing FlexColumn TUI application");
        _scrollbackTerminal.Initialize();

        // Initialize state manager with state factories
        RegisterStateFactories();
        await _stateManager.InitializeAsync(_tuiContext);

        LoadCommandHistory();
        RenderInitialContent();
        _logger.LogDebug("FlexColumn TUI application initialized");
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
        return _stateManager.RenderDynamicContent();
    }


    private IRenderable CreateWelcomeMessage()
    {
        var contentItems = new List<IRenderable>
        {
            // Shadow effect style with multi-color letters - "Mogzi" with each letter different color
            new Markup("[bold blue]███╗   ███╗[/] [bold cyan] ██████╗ [/] [bold green] ██████╗ [/] [bold yellow]███████╗[/] [bold magenta]██╗[/]"),
            new Markup("[bold blue]████╗ ████║[/] [bold cyan]██╔═══██╗[/] [bold green]██╔════╝ [/] [bold yellow]╚══███╔╝[/] [bold magenta]██║[/]"),
            new Markup("[bold blue]██╔████╔██║[/] [bold cyan]██║   ██║[/] [bold green]██║  ███╗[/] [bold yellow]  ███╔╝ [/] [bold magenta]██║[/]"),
            new Markup("[bold blue]██║╚██╔╝██║[/] [bold cyan]██║   ██║[/] [bold green]██║   ██║[/] [bold yellow] ███╔╝  [/] [bold magenta]██║[/]"),
            new Markup("[bold blue]██║ ╚═╝ ██║[/] [bold cyan]╚██████╔╝[/] [bold green]╚██████╔╝[/] [bold yellow]███████╗[/] [bold magenta]██║[/]"),
            new Markup("[bold blue]╚═╝     ╚═╝[/] [bold cyan] ╚═════╝ [/] [bold green] ╚═════╝ [/] [bold yellow]╚══════╝[/] [bold magenta]╚═╝[/]"),
            new Text(""),
            new Markup("[bold cyan]◢◤◢◤◢◤ Now connected to your Multi-model Autonomous Assistant ◢◤◢◤◢◤[/]"),
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
            await _stateManager.HandleKeyPressAsync(e);
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
            await _stateManager.HandleCharacterTypedAsync(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character typed: {Character}", e.Character);
        }
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
        _tuiContext.AiOperationCts?.Dispose();

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

            _tuiContext.CommandHistory.Clear();
            _tuiContext.CommandHistory.AddRange(userMessages);

            _logger?.LogDebug("Loaded {Count} commands from history", _tuiContext.CommandHistory.Count);
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

    private async Task SubmitCurrentInput()
    {
        var inputContext = _tuiContext.InputContext;
        if (string.IsNullOrWhiteSpace(inputContext.CurrentInput) || _stateManager.CurrentStateType != ChatState.Input)
        {
            return;
        }

        var inputToSubmit = inputContext.CurrentInput;
        AddToCommandHistory(inputToSubmit);
        inputContext.Clear();
        _tuiContext.CommandHistoryIndex = -1;

        await ProcessUserInput(inputToSubmit);
    }

    private void AddToCommandHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        if (_tuiContext.CommandHistory.Contains(command))
        {
            return;
        }

        _tuiContext.CommandHistory.Add(command);

        if (_tuiContext.CommandHistory.Count > 100)
        {
            _tuiContext.CommandHistory.RemoveAt(0);
        }

        _tuiContext.CommandHistoryIndex = -1;
    }

    private async Task ProcessUserInput(string input)
    {
        try
        {
            // Add spacing before user message
            _scrollbackTerminal.WriteStatic(new Markup(""));

            var appService = _tuiContext.AppService;
            var workingDirectoryProvider = _tuiContext.WorkingDirectoryProvider;
            var slashCommandProcessor = _tuiContext.SlashCommandProcessor;

            // Get current environment context
            var envPrompt = EnvSystemPrompt.GetEnvPrompt(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                appService.ChatClient.OperatingSystem.ToString(),
                appService.ChatClient.DefaultShell,
                appService.ChatClient.Username,
                appService.ChatClient.Hostname,
                workingDirectoryProvider.GetCurrentDirectory(),
                "chat", // mode - could be made configurable
                appService.ChatClient.Config.ToolApprovals
            );

            _logger?.LogDebug("Generated environment prompt: {EnvPrompt}", envPrompt);

            // Create user message with environment context appended (for AI processing)
            var fullUserMessage = Mogzi.Utils.MessageUtils.AppendSystemEnvironment(input, envPrompt);
            var userMessage = new ChatMessage(ChatRole.User, fullUserMessage);
            _historyManager.AddUserMessage(userMessage);

            _logger?.LogDebug("Full user message (with env context) length: {Length}", fullUserMessage.Length);
            _logger?.LogDebug("Original user input: {Input}", input);

            // Display only the original user input (stripped of env context)
            var displayMessage = new ChatMessage(ChatRole.User, input);
            _scrollbackTerminal.WriteStatic(RenderMessage(displayMessage));

            if (slashCommandProcessor.TryProcessCommand(input, out var commandOutput))
            {
                if (_tuiContext.InputContext.State == InputState.UserSelection)
                {
                    // Command is interactive, so we don't process it as a chat message.
                    return;
                }

                if (!string.IsNullOrEmpty(commandOutput))
                {
                    var commandMessage = new ChatMessage(ChatRole.Assistant, commandOutput);
                    _historyManager.AddAssistantMessage(commandMessage);
                    _scrollbackTerminal.WriteStatic(RenderMessage(commandMessage));
                }
                return;
            }

            // Create new cancellation token for this AI operation
            _tuiContext.AiOperationCts?.Dispose();
            _tuiContext.AiOperationCts = new CancellationTokenSource();
            _tuiContext.AiOperationStartTime = DateTime.Now;

            await _tuiContext.RequestStateTransitionAsync(ChatState.Thinking);

            var chatHistory = _historyManager.GetCurrentChatHistory();
            var responseStream = appService.ProcessChatMessageAsync(chatHistory, _tuiContext.AiOperationCts.Token);

            var assistantMessage = new ChatMessage(ChatRole.Assistant, "");
            _historyManager.AddAssistantMessage(assistantMessage);
            _scrollbackTerminal.WriteStatic(new Markup(""));
            _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage), isUpdatable: true);

            await foreach (var responseUpdate in responseStream)
            {
                if (!string.IsNullOrEmpty(responseUpdate.Text))
                {
                    var newText = assistantMessage.Text + responseUpdate.Text;
                    _logger?.LogInformation($"ChatMsg[Assistant, '{newText}'");
                    assistantMessage = new ChatMessage(ChatRole.Assistant, newText);
                    _historyManager.UpdateLastMessage(assistantMessage);
                    _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage), isUpdatable: true);
                }

                if (IsToolExecutionUpdate(responseUpdate))
                {
                    await _tuiContext.RequestStateTransitionAsync(ChatState.ToolExecution);

                    // Extract tool name first
                    ExtractToolNameFromUpdate(responseUpdate);

                    // Set progress text based on available information
                    if (!string.IsNullOrWhiteSpace(responseUpdate.Text))
                    {
                        _tuiContext.ToolProgress = responseUpdate.Text;
                    }
                    else if (!string.IsNullOrWhiteSpace(_tuiContext.CurrentToolName))
                    {
                        _tuiContext.ToolProgress = $"Executing {_tuiContext.CurrentToolName}...";
                        _scrollbackTerminal.WriteStatic(new Markup($"[green]•[/] [dim]{_tuiContext.ToolProgress}[/]"));
                    }

                    _logger?.LogInformation($"ChatMsg[Tool, '{_tuiContext.ToolProgress}'");

                    // Handle tool result display
                    await HandleToolExecutionResult(responseUpdate);
                }
            }
            _scrollbackTerminal.WriteStatic(new Markup(""));
            _logger?.LogInformation($"ChatMsg[Assistant, '{assistantMessage.Text}'");
            _scrollbackTerminal.WriteStatic(RenderMessage(assistantMessage));
        }
        catch (OperationCanceledException) when (_tuiContext.AiOperationCts?.Token.IsCancellationRequested == true)
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
            await _tuiContext.RequestStateTransitionAsync(ChatState.Input);
            _tuiContext.ToolProgress = string.Empty;
            _tuiContext.CurrentToolName = string.Empty;
        }
    }

    private bool IsToolExecutionUpdate(ChatResponseUpdate responseUpdate)
    {
        if (responseUpdate.Contents == null)
        {
            // Check for tool response XML in text content even when Contents is null
            if (!string.IsNullOrEmpty(responseUpdate.Text))
            {
                var hasToolResponse = responseUpdate.Text.Contains("<tool_response") || responseUpdate.Text.Contains("</tool_response>");
                return hasToolResponse;
            }

            return false;
        }

        // Check for function call/result content
        var hasFunctionContent = responseUpdate.Contents.Any(content => content is FunctionCallContent or FunctionResultContent);
        if (hasFunctionContent)
        {
            return true;
        }

        // Check for tool response XML in text content
        if (!string.IsNullOrEmpty(responseUpdate.Text))
        {
            var hasToolResponse = responseUpdate.Text.Contains("<tool_response") || responseUpdate.Text.Contains("</tool_response>");
            return hasToolResponse;
        }

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
                _tuiContext.CurrentToolName = !string.IsNullOrEmpty(keyValue) ? $"{toolName} → {keyValue}" : toolName;
            }
            else
            {
                _tuiContext.CurrentToolName = toolName;
            }
        }
        else
        {
            _tuiContext.CurrentToolName = string.Empty;
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


    private async Task HandleToolExecutionResult(ChatResponseUpdate responseUpdate)
    {
        try
        {
            if (responseUpdate.Contents == null && string.IsNullOrEmpty(responseUpdate.Text))
            {
                return;
            }

            string? toolName = null;
            string? result = null;

            // Handle function call content (tool execution starting)
            var functionCall = responseUpdate.Contents?.OfType<FunctionCallContent>().FirstOrDefault();
            if (functionCall != null)
            {
                // Store the mapping of call ID to tool name for later use
                _tuiContext.FunctionCallToToolName[functionCall.CallId] = functionCall.Name;

                // For EditTool, capture the pre-edit content (Gemini CLI approach)
                await CapturePreEditContentForEditTool(functionCall);
            }

            // Handle function result content (tool execution completed)
            var functionResult = responseUpdate.Contents?.OfType<FunctionResultContent>().FirstOrDefault();

            if (functionResult != null)
            {
                // Get the tool name from our stored mapping
                _ = _tuiContext.FunctionCallToToolName.TryGetValue(functionResult.CallId, out toolName);
                toolName ??= "Unknown Tool";

                result = functionResult.Result?.ToString() ?? "";
            }
            // If we only have a function call but no result yet, don't process for display
            else if (functionCall != null && functionResult == null)
            {
                return;
            }
            // Handle XML tool responses in text content
            else if (!string.IsNullOrEmpty(responseUpdate.Text) &&
                     (responseUpdate.Text.Contains("<tool_response") || responseUpdate.Text.Contains("</tool_response>")))
            {
                result = responseUpdate.Text;

                // Extract tool name from XML if possible
                try
                {
                    var toolNameMatch = System.Text.RegularExpressions.Regex.Match(result, @"tool_name=""([^""]+)""");
                    toolName = toolNameMatch.Success ? toolNameMatch.Groups[1].Value : "Unknown Tool";
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Failed to extract tool name from XML");
                    toolName = "Unknown Tool";
                }
            }

            // If we have a tool result to display
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(toolName))
            {
                // Parse the tool response for enhanced display
                var toolInfo = _tuiContext.ToolResponseParser.ParseToolResponse(toolName, result);

                // Handle different tool types appropriately
                UnifiedDiff? diff = null;
                string? displayContent = null;

                try
                {
                    var normalizedToolName = toolInfo.ToolName.ToLowerInvariant();

                    // For WriteFileTool - show content directly, no diff
                    if (IsWriteFileTool(normalizedToolName))
                    {
                        displayContent = toolInfo.NewContent ?? ExtractContentFromXml(result);
                    }
                    // For EditTool and DiffPatchTools - generate/extract diffs
                    else if (IsEditTool(normalizedToolName) || IsDiffPatchTool(normalizedToolName))
                    {
                        if (!string.IsNullOrEmpty(toolInfo.FilePath))
                        {
                            string? originalContent = null;
                            var newContent = toolInfo.NewContent;

                            // For EditTool, use the captured pre-edit content (Gemini CLI approach)
                            if (IsEditTool(normalizedToolName) && functionResult != null)
                            {
                                if (_tuiContext.FunctionCallToPreEditContent.TryGetValue(functionResult.CallId, out var preEditContent))
                                {
                                    originalContent = preEditContent;
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
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogDebug(ex, "Could not read original file content for diff: {FilePath}", toolInfo.FilePath);
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
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogDebug(ex, "Could not read current file content: {FilePath}", toolInfo.FilePath);
                                }
                            }

                            // Set up logger for UnifiedDiffGenerator debugging
                            UnifiedDiffGenerator.SetLogger(_logger);

                            // Generate diff using the tool response parser
                            diff = _tuiContext.ToolResponseParser.ExtractFileDiff(
                                toolInfo.ToolName,
                                result,
                                originalContent,
                                newContent,
                                toolInfo.FilePath);
                        }
                    }
                    else
                    {
                        displayContent = toolInfo.Summary;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Could not process tool result for display");
                }

                // Create enhanced tool display with clean styling
                var toolDisplay = ToolExecutionDisplay.CreateToolDisplay(
                    toolInfo.ToolName,
                    toolInfo.Status,
                    toolInfo.Description,
                    diff: diff,
                    result: displayContent ?? toolInfo.Summary ?? result
                );

                // Display the tool execution result in scrollback
                _scrollbackTerminal.WriteStatic(toolDisplay);
                _scrollbackTerminal.WriteStatic(new Markup(""));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling tool execution result");
        }

        await Task.CompletedTask;
    }

    private static bool IsWriteFileTool(string normalizedToolName)
    {
        return normalizedToolName is "write_file" or "writefile" or "write_to_file";
    }

    private static bool IsEditTool(string normalizedToolName)
    {
        return normalizedToolName is "replace_in_file" or "edit_file" or "editfile" or "edit";
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
                            _tuiContext.FunctionCallToPreEditContent[functionCall.CallId] = preEditContent;
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
                        _tuiContext.FunctionCallToPreEditContent[functionCall.CallId] = string.Empty;
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

        // For EditTool (replace_in_file), extract the old_string parameter
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
