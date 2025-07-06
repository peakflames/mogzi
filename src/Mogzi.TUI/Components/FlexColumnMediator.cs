namespace Mogzi.TUI.Components;

/// <summary>
/// Coordinates interactions between TUI components and manages complex workflows.
/// Implements the mediator pattern to reduce coupling between components.
/// </summary>
public class FlexColumnMediator(ILogger<FlexColumnMediator> logger, IThemeInfo themeInfo) : ITuiMediator
{
    private readonly Dictionary<string, ITuiComponent> _registeredComponents = [];
    private readonly ILogger<FlexColumnMediator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IThemeInfo _themeInfo = themeInfo ?? throw new ArgumentNullException(nameof(themeInfo));

    public string Name => "FlexColumnMediator";

    public async Task HandleUserInputAsync(string input, ITuiContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogTrace("Mediator handling user input: {Input}", input);

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

            _logger?.LogTrace("Generated environment prompt: {EnvPrompt}", envPrompt);

            // Create user message with environment context appended (for AI processing)
            var fullUserMessage = Mogzi.Utils.MessageUtils.AppendSystemEnvironment(input, envPrompt);
            var userMessage = new ChatMessage(ChatRole.User, fullUserMessage);
            context.HistoryManager.AddUserMessage(userMessage);

            _logger?.LogTrace("Full user message (with env context) length: {Length}", fullUserMessage.Length);
            _logger?.LogTrace("Original user input: {Input}", input);

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

        _logger.LogTrace("Mediator handling state change from {PreviousState} to {NewState}", previousState, newState);

        // Notify relevant components about state changes
        await NotifyComponentAsync("ProgressPanel", new { NewState = newState, PreviousState = previousState }, context);
        await NotifyComponentAsync("InputPanel", new { NewState = newState, PreviousState = previousState }, context);
        await NotifyComponentAsync("FooterPanel", new { NewState = newState, PreviousState = previousState }, context);
    }

    public async Task HandleToolExecutionAsync(string toolName, string progress, ITuiContext context)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolName);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogTrace("Mediator handling tool execution: {ToolName} - {Progress}", toolName, progress);

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
                _logger.LogTrace("Notified component {ComponentName} with event data", componentName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying component {ComponentName}", componentName);
            }
        }
        else
        {
            _logger.LogTrace("Component {ComponentName} not registered for notifications", componentName);
        }
    }

    public void RegisterComponent(ITuiComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        _registeredComponents[component.Name] = component;
        _logger.LogTrace("Registered component with mediator: {ComponentName}", component.Name);
    }

    public void UnregisterComponent(ITuiComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_registeredComponents.Remove(component.Name))
        {
            _logger.LogTrace("Unregistered component from mediator: {ComponentName}", component.Name);
        }
    }

    public async Task NotifyHistoryChangedAsync()
    {
        // This method is called by the HistoryManager when the chat history changes.
        // In the current architecture, the main render loop runs continuously,
        // so we don't need to do anything here to trigger a re-render.
        // This hook is here to fulfill the architectural pattern and for future use,
        // e.g., if we move to a more event-driven rendering model.
        await Task.CompletedTask;
    }

    private IRenderContext CreateRenderContext(ITuiContext context)
    {
        var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();

        return new RenderContext(
            context,
            ChatState.Input, // Default state for render context
            _logger,
            context.ServiceProvider,
            renderingUtilities,
            _themeInfo
        );
    }

    public async Task StartAiProcessingWorkflow(ITuiContext context)
    {
        _logger.LogTrace("=== StartAiProcessingWorkflow BEGIN ===");
        _logger.LogTrace("AiOperationCts is null: {IsNull}", context.AiOperationCts == null);

        try
        {
            // Check if AiOperationCts is null and initialize if needed
            if (context.AiOperationCts == null)
            {
                _logger.LogTrace("AiOperationCts is null, initializing new CancellationTokenSource");
                context.AiOperationCts = new CancellationTokenSource();
                context.AiOperationStartTime = DateTime.Now;
                _logger.LogTrace("AiOperationCts initialized successfully");
            }
            else
            {
                _logger.LogTrace("AiOperationCts already exists, using existing instance");
            }

            _logger.LogTrace("Getting chat history from HistoryManager");
            var chatHistory = context.HistoryManager.GetCurrentChatHistory();
            _logger.LogTrace("Chat history retrieved, message count: {Count}", chatHistory.Count);

            _logger.LogTrace("Starting ProcessChatMessageAsync with AiOperationCts.Token");
            var responseStream = context.AppService.ProcessChatMessageAsync(chatHistory, context.AiOperationCts.Token);
            _logger.LogTrace("ProcessChatMessageAsync started successfully");

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
                    ExtractToolNameFromUpdate(context, responseUpdate);

                    // Set progress text for dynamic display (shown in ToolExecutionTuiState)
                    if (!string.IsNullOrWhiteSpace(responseUpdate.Text))
                    {
                        context.ToolProgress = responseUpdate.Text;
                    }
                    else if (!string.IsNullOrWhiteSpace(context.CurrentToolName))
                    {
                        context.ToolProgress = $"Executing {context.CurrentToolName}...";
                        // Don't write progress to static area - it will be shown in dynamic area by ToolExecutionTuiState
                    }

                    _logger?.LogInformation($"ChatMsg[Tool, '{context.ToolProgress}'");

                    // Handle tool result display (this writes final results to static area)
                    await HandleToolExecutionResult(context, responseUpdate);
                }
            }

            context.ScrollbackTerminal.WriteStatic(new Markup(""));
            _logger?.LogInformation($"ChatMsg[Assistant, '{assistantMessage.Text}'");
            _logger?.LogTrace("=== WRITING FINAL NON-UPDATABLE ASSISTANT MESSAGE ===");
            _logger?.LogTrace("Final assistant message content: {Content}", assistantMessage.Text);
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(assistantMessage));
            _logger?.LogTrace("=== FINAL NON-UPDATABLE ASSISTANT MESSAGE WRITTEN ===");
        }
        catch (OperationCanceledException) when (context.AiOperationCts?.Token.IsCancellationRequested == true)
        {
            _logger?.LogTrace("AI operation was cancelled by user");
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

    private void ExtractToolNameFromUpdate(ITuiContext context, ChatResponseUpdate responseUpdate)
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

            // Store the mapping of call ID to tool name for later use
            context.FunctionCallToToolName[functionCall.CallId] = functionCall.Name!;

            // For EditTool, capture the pre-edit content
            _ = CapturePreEditContentForEditTool(context, functionCall);
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

    private async Task CapturePreEditContentForEditTool(ITuiContext context, FunctionCallContent functionCall)
    {
        context.Logger?.LogTrace("=== CapturePreEditContentForEditTool START ===");

        try
        {
            // Only capture for EditTool
            if (!IsEditTool(functionCall.Name.ToLowerInvariant()))
            {
                context.Logger?.LogTrace("Not an EditTool, skipping pre-edit capture");
                return;
            }

            // Extract file_path from function arguments
            if (functionCall.Arguments?.TryGetValue("file_path", out var filePathValue) == true)
            {
                var filePath = filePathValue?.ToString();
                if (!string.IsNullOrEmpty(filePath))
                {
                    context.Logger?.LogTrace("Capturing pre-edit content for file: {FilePath}", filePath);

                    // Read the entire file content before the edit
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            var preEditContent = await File.ReadAllTextAsync(filePath);
                            context.FunctionCallToPreEditContent[functionCall.CallId] = preEditContent;
                            context.Logger?.LogTrace("Captured pre-edit content, length: {Length}", preEditContent.Length);
                        }
                        catch (Exception ex)
                        {
                            context.Logger?.LogTrace(ex, "Failed to read pre-edit content from file: {FilePath}", filePath);
                        }
                    }
                    else
                    {
                        context.Logger?.LogTrace("File does not exist, storing empty pre-edit content: {FilePath}", filePath);
                        context.FunctionCallToPreEditContent[functionCall.CallId] = string.Empty;
                    }
                }
                else
                {
                    context.Logger?.LogTrace("File path is null or empty");
                }
            }
            else
            {
                context.Logger?.LogTrace("No file_path argument found in function call");
            }
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(ex, "Error capturing pre-edit content");
        }

        context.Logger?.LogTrace("=== CapturePreEditContentForEditTool END ===");
    }

    private static bool IsEditTool(string normalizedToolName)
    {
        return normalizedToolName is "replace_in_file" or "edit_file" or "editfile" or "edit";
    }

    private async Task HandleToolExecutionResult(ITuiContext context, ChatResponseUpdate responseUpdate)
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

            // Handle function result content (tool execution completed)
            var functionResult = responseUpdate.Contents?.OfType<FunctionResultContent>().FirstOrDefault();

            if (functionResult != null)
            {
                // Get the tool name from our stored mapping
                _ = context.FunctionCallToToolName.TryGetValue(functionResult.CallId, out toolName);
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
                    context.Logger?.LogTrace(ex, "Failed to extract tool name from XML");
                    toolName = "Unknown Tool";
                }
            }

            // If we have a tool result to display
            if (!string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(toolName))
            {
                // Parse the tool response for enhanced display
                var toolInfo = context.ToolResponseParser.ParseToolResponse(toolName, result);

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

                            // For EditTool, use the captured pre-edit content
                            if (IsEditTool(normalizedToolName) && functionResult != null)
                            {
                                if (context.FunctionCallToPreEditContent.TryGetValue(functionResult.CallId, out var preEditContent))
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
                                        context.Logger?.LogTrace(ex, "Could not read original file content for diff: {FilePath}", toolInfo.FilePath);
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
                                    context.Logger?.LogTrace(ex, "Could not read current file content: {FilePath}", toolInfo.FilePath);
                                }
                            }

                            // Set up logger for UnifiedDiffGenerator debugging
                            UnifiedDiffGenerator.SetLogger(context.Logger);

                            // Generate diff using the tool response parser
                            diff = context.ToolResponseParser.ExtractFileDiff(
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
                    context.Logger?.LogTrace(ex, "Could not process tool result for display");
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
                context.ScrollbackTerminal.WriteStatic(toolDisplay);
                context.ScrollbackTerminal.WriteStatic(new Markup(""));
            }
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(ex, "Error handling tool execution result");
        }

        await Task.CompletedTask;
    }

    private static bool IsWriteFileTool(string normalizedToolName)
    {
        return normalizedToolName is "write_file" or "writefile" or "write_to_file";
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




}
