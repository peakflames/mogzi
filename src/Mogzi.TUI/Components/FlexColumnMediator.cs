namespace Mogzi.TUI.Components;

/// <summary>
/// Coordinates interactions between TUI components and manages complex workflows.
/// Implements the mediator pattern to reduce coupling between components.
/// </summary>
public class FlexColumnMediator(ILogger<FlexColumnMediator> logger) : ITuiMediator
{
    private readonly Dictionary<string, ITuiComponent> _registeredComponents = [];
    private readonly ILogger<FlexColumnMediator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string Name => "FlexColumnMediator";

    public async Task HandleUserInputAsync(string input, ITuiContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("Mediator handling user input: {Input}", input);

        try
        {
            // Add spacing before user message
            context.ScrollbackTerminal.WriteStatic(new Markup(""));

            // Get current environment context
            var envPrompt = EnvSystemPrompt.GetEnvPrompt(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                context.AppService.ChatClient.OperatingSystem.ToString(),
                context.AppService.ChatClient.DefaultShell,
                context.AppService.ChatClient.Username,
                context.AppService.ChatClient.Hostname,
                context.WorkingDirectoryProvider.GetCurrentDirectory(),
                "chat", // mode - could be made configurable
                context.AppService.ChatClient.Config.ToolApprovals
            );

            _logger?.LogDebug("Generated environment prompt: {EnvPrompt}", envPrompt);

            // Create user message with environment context appended (for AI processing)
            var fullUserMessage = Mogzi.Utils.MessageUtils.AppendSystemEnvironment(input, envPrompt);
            var userMessage = new ChatMessage(ChatRole.User, fullUserMessage);
            context.HistoryManager.AddUserMessage(userMessage);

            _logger?.LogDebug("Full user message (with env context) length: {Length}", fullUserMessage.Length);
            _logger?.LogDebug("Original user input: {Input}", input);

            // Display only the original user input (stripped of env context)
            var displayMessage = new ChatMessage(ChatRole.User, input);
            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(displayMessage));

            if (context.SlashCommandProcessor.TryProcessCommand(input, out var commandOutput))
            {
                if (context.InputContext.State == InputState.UserSelection)
                {
                    // Command is interactive, so we don't process it as a chat message.
                    return;
                }

                if (!string.IsNullOrEmpty(commandOutput))
                {
                    var commandMessage = new ChatMessage(ChatRole.Assistant, commandOutput);
                    context.HistoryManager.AddAssistantMessage(commandMessage);
                    context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(commandMessage));
                }
                return;
            }

            // Create new cancellation token for this AI operation
            context.AiOperationCts?.Dispose();
            context.AiOperationCts = new CancellationTokenSource();
            context.AiOperationStartTime = DateTime.Now;

            // Transition to thinking state and start AI processing
            await context.RequestStateTransitionAsync(ChatState.Thinking);

            // Start AI processing workflow
            await StartAiProcessingWorkflow(context);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in mediator handling user input");
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing input: {ex.Message}");
            context.HistoryManager.AddAssistantMessage(errorMessage);
            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(errorMessage));
        }
    }

    public async Task HandleKeyPressAsync(KeyPressEventArgs e, ITuiContext context)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(context);

        // Delegate to the current state for handling
        // The mediator coordinates but doesn't replace state-specific logic
        await Task.CompletedTask;
    }

    public async Task HandleCharacterTypedAsync(CharacterTypedEventArgs e, ITuiContext context)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(context);

        // Delegate to the current state for handling
        // The mediator coordinates but doesn't replace state-specific logic
        await Task.CompletedTask;
    }

    public async Task HandleStateChangeAsync(ChatState newState, ChatState previousState, ITuiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("Mediator handling state change from {PreviousState} to {NewState}", previousState, newState);

        // Notify relevant components about state changes
        await NotifyComponentAsync("ProgressPanel", new { NewState = newState, PreviousState = previousState }, context);
        await NotifyComponentAsync("InputPanel", new { NewState = newState, PreviousState = previousState }, context);
        await NotifyComponentAsync("FooterPanel", new { NewState = newState, PreviousState = previousState }, context);
    }

    public async Task HandleToolExecutionAsync(string toolName, string progress, ITuiContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogDebug("Mediator handling tool execution: {ToolName} - {Progress}", toolName, progress);

        // Update context with tool information
        context.CurrentToolName = toolName;
        context.ToolProgress = progress;

        // Transition to tool execution state if not already there
        if (context.InputContext.State != InputState.Normal) // Assuming we're not in input state during tool execution
        {
            await context.RequestStateTransitionAsync(ChatState.ToolExecution);
        }

        // Notify progress panel about tool execution
        await NotifyComponentAsync("ProgressPanel", new { ToolName = toolName, Progress = progress }, context);
    }

    public async Task NotifyComponentAsync(string componentName, object eventData, ITuiContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentName);
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(context);

        if (_registeredComponents.TryGetValue(componentName, out var component))
        {
            try
            {
                var renderContext = CreateRenderContext(context);
                _ = await component.HandleInputAsync(renderContext, eventData);
                _logger.LogDebug("Notified component {ComponentName} with event data", componentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying component {ComponentName}", componentName);
            }
        }
        else
        {
            _logger.LogDebug("Component {ComponentName} not registered for notifications", componentName);
        }
    }

    public void RegisterComponent(ITuiComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        _registeredComponents[component.Name] = component;
        _logger.LogDebug("Registered component with mediator: {ComponentName}", component.Name);
    }

    public void UnregisterComponent(ITuiComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_registeredComponents.Remove(component.Name))
        {
            _logger.LogDebug("Unregistered component from mediator: {ComponentName}", component.Name);
        }
    }

    private async Task StartAiProcessingWorkflow(ITuiContext context)
    {
        try
        {
            var chatHistory = context.HistoryManager.GetCurrentChatHistory();
            var responseStream = context.AppService.ProcessChatMessageAsync(chatHistory, context.AiOperationCts!.Token);

            var assistantMessage = new ChatMessage(ChatRole.Assistant, "");
            context.HistoryManager.AddAssistantMessage(assistantMessage);
            context.ScrollbackTerminal.WriteStatic(new Markup(""));

            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(assistantMessage), isUpdatable: true);

            await foreach (var responseUpdate in responseStream)
            {
                if (!string.IsNullOrEmpty(responseUpdate.Text))
                {
                    var newText = assistantMessage.Text + responseUpdate.Text;
                    _logger?.LogInformation($"ChatMsg[Assistant, '{newText}'");
                    assistantMessage = new ChatMessage(ChatRole.Assistant, newText);
                    context.HistoryManager.UpdateLastMessage(assistantMessage);
                    context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(assistantMessage), isUpdatable: true);
                }

                if (IsToolExecutionUpdate(responseUpdate))
                {
                    await context.RequestStateTransitionAsync(ChatState.ToolExecution);

                    // Extract tool name and handle tool execution
                    ExtractToolNameFromUpdate(responseUpdate, context);

                    // Set progress text based on available information
                    if (!string.IsNullOrWhiteSpace(responseUpdate.Text))
                    {
                        context.ToolProgress = responseUpdate.Text;
                    }
                    else if (!string.IsNullOrWhiteSpace(context.CurrentToolName))
                    {
                        context.ToolProgress = $"Executing {context.CurrentToolName}...";
                        context.ScrollbackTerminal.WriteStatic(new Markup($"[green]•[/] [dim]{context.ToolProgress}[/]"));
                    }

                    _logger?.LogInformation($"ChatMsg[Tool, '{context.ToolProgress}'");

                    // Handle tool result display
                    await HandleToolExecutionResult(responseUpdate, context);
                }
            }

            context.ScrollbackTerminal.WriteStatic(new Markup(""));
            _logger?.LogInformation($"ChatMsg[Assistant, '{assistantMessage.Text}'");
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(assistantMessage));
        }
        catch (OperationCanceledException) when (context.AiOperationCts?.Token.IsCancellationRequested == true)
        {
            _logger?.LogDebug("AI operation was cancelled by user");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AI processing workflow");
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing request: {ex.Message}");
            context.HistoryManager.AddAssistantMessage(errorMessage);
            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(errorMessage));
        }
        finally
        {
            await context.RequestStateTransitionAsync(ChatState.Input);
            context.ToolProgress = string.Empty;
            context.CurrentToolName = string.Empty;
        }
    }

    private IRenderContext CreateRenderContext(ITuiContext context)
    {
        var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
        var themeInfo = new DefaultThemeInfo();

        return new RenderContext(
            context,
            ChatState.Input, // Default state for render context
            _logger,
            context.ServiceProvider,
            renderingUtilities,
            themeInfo
        );
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

    private void ExtractToolNameFromUpdate(ChatResponseUpdate responseUpdate, ITuiContext context)
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
                context.CurrentToolName = !string.IsNullOrEmpty(keyValue) ? $"{toolName} → {keyValue}" : toolName;
            }
            else
            {
                context.CurrentToolName = toolName;
            }
        }
        else
        {
            context.CurrentToolName = string.Empty;
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

    private async Task HandleToolExecutionResult(ChatResponseUpdate responseUpdate, ITuiContext context)
    {
        // This is a simplified version - the full implementation would be similar to the original
        // For now, just log that tool execution is happening
        _logger.LogDebug("Handling tool execution result in mediator");
        await Task.CompletedTask;
    }
}
