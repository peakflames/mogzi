namespace Mogzi.TUI.State;

#pragma warning disable IDE0010 // Add missing cases

/// <summary>
/// Handles the thinking state of the TUI application.
/// Manages AI processing state with animation and cancellation support.
/// </summary>
public class ThinkingTuiState : ITuiState
{
    public string Name => "Thinking";

    public IRenderable RenderDynamicContent(ITuiContext context)
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
        var duration = (int)(DateTime.Now - context.AiOperationStartTime).TotalSeconds;
        var durationText = duration > 0 ? $"{duration}s" : "0s";

        var thinkingComponent = new Panel(new Markup($"[orange3]{leadingAnimation}[/] [dim]Thinking (esc to cancel, {durationText})[/]"))
            .NoBorder();

        return new Rows(new Text(""), thinkingComponent, new Text(""), CreateFlexFooterComponent(context));
    }

    public async Task HandleKeyPressAsync(ITuiContext context, KeyPressEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        try
        {
            switch (e.Key)
            {
                case ConsoleKey.Escape:
                    await InterruptAiOperation(context);
                    e.Handled = true;
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error handling key press in ThinkingTuiState: {Key}", e.Key);
        }
    }

    public Task HandleCharacterTypedAsync(ITuiContext context, CharacterTypedEventArgs e)
    {
        // No character input handling in thinking state
        return Task.CompletedTask;
    }

    public async Task OnEnterAsync(ITuiContext context, ITuiState? previousState)
    {
        context.Logger.LogTrace("=== ThinkingTuiState.OnEnterAsync BEGIN ===");
        context.Logger.LogTrace("Entering ThinkingTuiState from {PreviousState}", previousState?.Name ?? "none");
        context.Logger.LogTrace("AiOperationCts is null: {IsNull}", context.AiOperationCts == null);

        try
        {
            // Start AI processing when entering thinking state
            context.Logger.LogTrace("Calling StartAiProcessingWorkflow from ThinkingTuiState.OnEnterAsync");
            await context.Mediator.StartAiProcessingWorkflow(context);
            context.Logger.LogTrace("StartAiProcessingWorkflow completed successfully");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error in ThinkingTuiState.OnEnterAsync");
            throw;
        }

        context.Logger.LogTrace("=== ThinkingTuiState.OnEnterAsync END ===");
    }

    public Task OnExitAsync(ITuiContext context, ITuiState? nextState)
    {
        context.Logger.LogTrace("Exiting ThinkingTuiState to {NextState}", nextState?.Name ?? "none");
        return Task.CompletedTask;
    }


    private async Task InterruptAiOperation(ITuiContext context)
    {
        try
        {
            // Cancel the AI operation
            context.AiOperationCts?.Cancel();

            // Clear any progress indicators
            context.ToolProgress = string.Empty;
            context.CurrentToolName = string.Empty;

            // Display interruption message without assistant prefix
            context.ScrollbackTerminal.WriteStatic(new Markup("⚠ Request cancelled."));
            context.ScrollbackTerminal.WriteStatic(new Markup(""));

            // Add to history as assistant message for context
            var interruptMessage = new ChatMessage(ChatRole.Assistant, "⚠ Request cancelled.");
            context.HistoryManager.AddAssistantMessage(interruptMessage);

            context.Logger?.LogInformation("AI operation interrupted by user (Escape key)");

            // Return to input state
            await context.RequestStateTransitionAsync(ChatState.Input);
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(ex, "Error interrupting AI operation");
        }
    }










    private IRenderable CreateFlexFooterComponent(ITuiContext context)
    {
        var currentDir = GetDisplayPath(context.WorkingDirectoryProvider.GetCurrentDirectory());
        var modelInfo = GetModelDisplayInfo(context);
        var tokenInfo = GetTokenUsageInfo(context);
        var content = $"[skyblue2]{currentDir}[/]  [rosybrown]{modelInfo}[/] [dim]({tokenInfo})[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }

    private string GetTokenUsageInfo(ITuiContext context)
    {
        try
        {
            var chatHistory = context.HistoryManager.GetCurrentChatHistory();
            var tokenCount = context.AppService.CalculateTokenMetrics(chatHistory);

            // Estimate context window size based on model (this could be made configurable)
            var contextWindowSize = EstimateContextWindowSize(context);
            var percentageUsed = Math.Min(100.0, tokenCount * 100.0 / contextWindowSize);
            var percentageLeft = 100.0 - percentageUsed;

            return $"{tokenCount:N0} tokens, {percentageLeft:F2}% context left";
        }
        catch (Exception ex)
        {
            context.Logger?.LogWarning(ex, "Error calculating token usage");
            return "token calculation unavailable";
        }
    }

    private int EstimateContextWindowSize(ITuiContext context)
    {
        try
        {
            var modelId = context.AppService.ChatClient.ActiveProfile.ModelId.ToLowerInvariant();

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
            context.Logger?.LogWarning(ex, "Error estimating context window size");
            return 128000; // Safe default
        }
    }

    private string GetModelDisplayInfo(ITuiContext context)
    {
        try
        {
            var chatClient = context.AppService.ChatClient;
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
            context.Logger?.LogWarning(ex, "Error getting model display info");
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
        catch
        {
            return fullPath;
        }
    }


}

#pragma warning restore IDE0010 // Add missing cases
