namespace Mogzi.TUI.State;

#pragma warning disable IDE0010 // Add missing cases
#pragma warning disable IDE0045 // Convert to conditional expression

/// <summary>
/// Handles the input state of the TUI application.
/// Manages normal input, autocomplete, and user selection modes.
/// </summary>
public class InputTuiState : ITuiState
{
    public string Name => "Input";

    public IRenderable RenderDynamicContent(ITuiContext context)
    {
        var bottomComponent = context.InputContext.State switch
        {
            InputState.UserSelection => CreateInputWithUserSelection(context),
            InputState.Autocomplete when context.InputContext.ShowSuggestions => CreateInputWithAutocomplete(context),
            _ => CreateFlexInputComponent(context)
        };

        return new Rows(new Text(""), bottomComponent, new Text(""), CreateFlexFooterComponent(context));
    }

    public async Task HandleKeyPressAsync(ITuiContext context, KeyPressEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        try
        {
            // Handle autocomplete navigation first
            if (context.InputContext.State == InputState.Autocomplete && context.InputContext.ShowSuggestions)
            {
                switch (e.Key)
                {
                    case ConsoleKey.UpArrow:
                        NavigateAutocomplete(context, up: true);
                        e.Handled = true;
                        return;

                    case ConsoleKey.DownArrow:
                        NavigateAutocomplete(context, up: false);
                        e.Handled = true;
                        return;

                    case ConsoleKey.Tab:
                    case ConsoleKey.Enter:
                        AcceptAutocompleteSuggestion(context);
                        e.Handled = true;
                        return;

                    case ConsoleKey.Escape:
                        CancelAutocomplete(context);
                        e.Handled = true;
                        return;
                }
            }
            else if (context.InputContext.State == InputState.UserSelection)
            {
                switch (e.Key)
                {
                    case ConsoleKey.UpArrow:
                        NavigateUserSelection(context, up: true);
                        e.Handled = true;
                        return;

                    case ConsoleKey.DownArrow:
                        NavigateUserSelection(context, up: false);
                        e.Handled = true;
                        return;

                    case ConsoleKey.Enter:
                        await context.UserSelectionManager.AcceptSelectionAsync();
                        e.Handled = true;
                        return;

                    case ConsoleKey.Escape:
                        context.UserSelectionManager.Deactivate();
                        e.Handled = true;
                        return;
                }
            }

            switch (e.Key)
            {
                case ConsoleKey.Enter:
                    await SubmitCurrentInput(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.UpArrow:
                    await NavigateCommandHistoryAsync(context, up: true);
                    e.Handled = true;
                    break;

                case ConsoleKey.DownArrow:
                    await NavigateCommandHistoryAsync(context, up: false);
                    e.Handled = true;
                    break;

                case ConsoleKey.LeftArrow:
                    await MoveCursorLeftAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.RightArrow:
                    await MoveCursorRightAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.Home:
                    await MoveCursorToStartAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.End:
                    await MoveCursorToEndAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.Backspace:
                    await DeleteCharacterBeforeAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.Delete:
                    await DeleteCharacterAfterAsync(context);
                    e.Handled = true;
                    break;

                case ConsoleKey.Escape:
                    ClearCurrentInput(context);
                    e.Handled = true;
                    break;
            }
        }
        catch
        {
            context.InputContext.ClearAutocomplete();
        }
    }

    public async Task HandleCharacterTypedAsync(ITuiContext context, CharacterTypedEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        try
        {
            context.Logger.LogTrace("InputTuiState handling character: {Character}", e.Character);
            await InsertCharacterAsync(context, e.Character);
            e.Handled = true;
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error handling character typed in InputTuiState: {Character}", e.Character);
        }
    }

    public Task OnEnterAsync(ITuiContext context, ITuiState? previousState)
    {
        context.Logger.LogTrace("Entering InputTuiState from {PreviousState}", previousState?.Name ?? "none");
        return Task.CompletedTask;
    }

    public Task OnExitAsync(ITuiContext context, ITuiState? nextState)
    {
        context.Logger.LogTrace("Exiting InputTuiState to {NextState}", nextState?.Name ?? "none");
        return Task.CompletedTask;
    }

    private IRenderable CreateFlexInputComponent(ITuiContext context)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var currentInput = context.InputContext.CurrentInput;

        string content;
        if (string.IsNullOrEmpty(currentInput))
        {
            content = $"{prompt}{cursor}[dim]Type your message or /help[/]";
        }
        else
        {
            // Insert cursor at the correct position
            var beforeCursor = currentInput[..context.InputContext.CursorPosition];
            var afterCursor = currentInput[context.InputContext.CursorPosition..];
            content = $"{prompt}{beforeCursor}{cursor}{afterCursor}";
        }

        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey23)
            .Padding(1, 0, 1, 0)
            .Expand();
    }

    private IRenderable CreateInputWithAutocomplete(ITuiContext context)
    {
        var inputPanel = CreateFlexInputComponent(context);

        if (!context.InputContext.ShowSuggestions || context.InputContext.Suggestions.Count == 0)
        {
            return inputPanel;
        }

        var suggestionItems = context.InputContext.Suggestions.Select((suggestion, index) =>
        {
            var isSelected = index == context.InputContext.SelectedSuggestionIndex;
            var style = isSelected ? "[blue on white]" : "[dim]";
            var prefix = isSelected ? ">" : " ";

            var description = context.SlashCommandProcessor.GetAllCommands()
                .GetValueOrDefault(suggestion, "");

            return new Markup($"{style}{prefix} {suggestion,-12} {description}[/]");
        }).ToArray();

        var suggestionsPanel = new Panel(new Rows(suggestionItems))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(0, 0);

        return new Rows(inputPanel, suggestionsPanel);
    }

    private IRenderable CreateInputWithUserSelection(ITuiContext context)
    {
        var inputPanel = CreateFlexInputComponent(context);

        if (context.InputContext.CompletionItems.Count == 0)
        {
            return inputPanel;
        }

        var selectionItems = context.InputContext.CompletionItems.Select((item, index) =>
        {
            var isSelected = index == context.InputContext.SelectedSuggestionIndex;
            var style = isSelected ? "[blue on white]" : "[dim]";
            var prefix = isSelected ? ">" : " ";

            return new Markup($"{style}{prefix} {item.Text,-12} {item.Description}[/]");
        }).ToArray();

        var selectionPanel = new Panel(new Rows(selectionItems))
            .Header("Select an option")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(0, 0);

        return new Rows(inputPanel, selectionPanel);
    }

    private IRenderable CreateFlexFooterComponent(ITuiContext context)
    {
        var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
        var currentDir = renderingUtilities.FormatDisplayPath(context.WorkingDirectoryProvider.GetCurrentDirectory());
        var modelInfo = renderingUtilities.FormatModelInfo(context.AppService);
        var chatHistory = context.HistoryManager.GetCurrentChatHistory();
        var tokenInfo = renderingUtilities.FormatTokenUsage(context.AppService, chatHistory);
        var content = $"[skyblue2]{currentDir}[/]  [rosybrown]{modelInfo}[/] [dim]({tokenInfo})[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }


    private async Task NavigateCommandHistoryAsync(ITuiContext context, bool up)
    {
        if (context.CommandHistory.Count == 0)
        {
            return;
        }

        if (up)
        {
            if (context.CommandHistoryIndex == -1)
            {
                context.CommandHistoryIndex = context.CommandHistory.Count - 1;
                context.InputContext.CurrentInput = context.CommandHistory[context.CommandHistoryIndex];
            }
            else if (context.CommandHistoryIndex > 0)
            {
                context.CommandHistoryIndex--;
                context.InputContext.CurrentInput = context.CommandHistory[context.CommandHistoryIndex];
            }
        }
        else
        {
            if (context.CommandHistoryIndex >= 0 && context.CommandHistoryIndex < context.CommandHistory.Count - 1)
            {
                context.CommandHistoryIndex++;
                context.InputContext.CurrentInput = context.CommandHistory[context.CommandHistoryIndex];
            }
            else if (context.CommandHistoryIndex == context.CommandHistory.Count - 1)
            {
                context.CommandHistoryIndex = -1;
                context.InputContext.CurrentInput = string.Empty;
            }
        }

        context.InputContext.CursorPosition = context.InputContext.CurrentInput.Length;
        await UpdateAutocompleteStateAsync(context);
    }

    private async Task SubmitCurrentInput(ITuiContext context)
    {
        if (string.IsNullOrWhiteSpace(context.InputContext.CurrentInput))
        {
            return;
        }

        var inputToSubmit = context.InputContext.CurrentInput;
        AddToCommandHistory(context, inputToSubmit);
        ClearCurrentInput(context);

        await ProcessUserInput(context, inputToSubmit);
    }

    private void AddToCommandHistory(ITuiContext context, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        if (context.CommandHistory.Contains(command))
        {
            return;
        }

        context.CommandHistory.Add(command);

        if (context.CommandHistory.Count > 100)
        {
            context.CommandHistory.RemoveAt(0);
        }

        context.CommandHistoryIndex = -1;
    }

    private async Task ProcessUserInput(ITuiContext context, string input)
    {
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

            context.Logger?.LogTrace("Generated environment prompt: {EnvPrompt}", envPrompt);

            // Create user message with environment context appended (for AI processing)
            var fullUserMessage = Mogzi.Utils.MessageUtils.AppendSystemEnvironment(input, envPrompt);
            var userMessage = new ChatMessage(ChatRole.User, fullUserMessage);
            context.HistoryManager.AddUserMessage(userMessage);

            context.Logger?.LogTrace("Full user message (with env context) length: {Length}", fullUserMessage.Length);
            context.Logger?.LogTrace("Original user input: {Input}", input);

            // Display only the original user input (stripped of env context)
            var displayMessage = new ChatMessage(ChatRole.User, input);
            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            var themeInfo = context.ServiceProvider.GetRequiredService<IThemeInfo>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(displayMessage, themeInfo));

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
                    var renderingUtils = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
                    var theme = context.ServiceProvider.GetRequiredService<IThemeInfo>();
                    context.ScrollbackTerminal.WriteStatic(renderingUtils.RenderMessage(commandMessage, theme));
                }
                return;
            }

            // Transition to thinking state and start AI processing
            await context.RequestStateTransitionAsync(ChatState.Thinking);
        }
        catch (Exception ex)
        {
            context.Logger?.LogError(ex, "Error processing user input");
            var errorMessage = new ChatMessage(ChatRole.Assistant, $"Error processing input: {ex.Message}");
            context.HistoryManager.AddAssistantMessage(errorMessage);
            var renderingUtilities = context.ServiceProvider.GetRequiredService<IRenderingUtilities>();
            var themeInfo = context.ServiceProvider.GetRequiredService<IThemeInfo>();
            context.ScrollbackTerminal.WriteStatic(renderingUtilities.RenderMessage(errorMessage, themeInfo));
        }
    }

    private async Task InsertCharacterAsync(ITuiContext context, char character)
    {
        var oldInput = context.InputContext.CurrentInput;
        context.InputContext.CursorPosition = Math.Max(0, Math.Min(context.InputContext.CursorPosition, context.InputContext.CurrentInput.Length));
        context.InputContext.CurrentInput = context.InputContext.CurrentInput.Insert(context.InputContext.CursorPosition, character.ToString());
        context.InputContext.CursorPosition++;
        context.CommandHistoryIndex = -1;
        context.Logger.LogTrace("Input buffer changed from '{OldInput}' to '{NewInput}'", oldInput, context.InputContext.CurrentInput);
        await UpdateAutocompleteStateAsync(context);
    }

    private async Task DeleteCharacterBeforeAsync(ITuiContext context)
    {
        if (context.InputContext.CurrentInput.Length == 0)
        {
            return;
        }

        context.InputContext.CursorPosition = Math.Max(0, Math.Min(context.InputContext.CursorPosition, context.InputContext.CurrentInput.Length));

        if (context.InputContext.CursorPosition > 0)
        {
            context.InputContext.CurrentInput = context.InputContext.CurrentInput.Remove(context.InputContext.CursorPosition - 1, 1);
            context.InputContext.CursorPosition--;
        }

        context.CommandHistoryIndex = -1;
        await UpdateAutocompleteStateAsync(context);
    }

    private async Task DeleteCharacterAfterAsync(ITuiContext context)
    {
        if (context.InputContext.CurrentInput.Length == 0)
        {
            return;
        }

        context.InputContext.CursorPosition = Math.Max(0, Math.Min(context.InputContext.CursorPosition, context.InputContext.CurrentInput.Length));

        if (context.InputContext.CursorPosition < context.InputContext.CurrentInput.Length)
        {
            context.InputContext.CurrentInput = context.InputContext.CurrentInput.Remove(context.InputContext.CursorPosition, 1);
        }

        context.CommandHistoryIndex = -1;
        await UpdateAutocompleteStateAsync(context);
    }

    private async Task MoveCursorLeftAsync(ITuiContext context)
    {
        if (context.InputContext.CursorPosition > 0)
        {
            context.InputContext.CursorPosition--;
            await UpdateAutocompleteStateAsync(context);
        }
    }

    private async Task MoveCursorRightAsync(ITuiContext context)
    {
        if (context.InputContext.CursorPosition < context.InputContext.CurrentInput.Length)
        {
            context.InputContext.CursorPosition++;
            await UpdateAutocompleteStateAsync(context);
        }
    }

    private async Task MoveCursorToStartAsync(ITuiContext context)
    {
        context.InputContext.CursorPosition = 0;
        await UpdateAutocompleteStateAsync(context);
    }

    private async Task MoveCursorToEndAsync(ITuiContext context)
    {
        context.InputContext.CursorPosition = context.InputContext.CurrentInput.Length;
        await UpdateAutocompleteStateAsync(context);
    }

    private void ClearCurrentInput(ITuiContext context)
    {
        context.InputContext.Clear();
        context.CommandHistoryIndex = -1;
    }

    private async Task UpdateAutocompleteStateAsync(ITuiContext context)
    {
        try
        {
            // Detect which provider should be triggered
            var provider = context.AutocompleteManager.DetectTrigger(context.InputContext.CurrentInput, context.InputContext.CursorPosition);

            if (provider != null)
            {
                context.InputContext.ActiveProvider = provider;
                await context.AutocompleteManager.UpdateSuggestionsAsync(context.InputContext);
            }
            else
            {
                context.InputContext.ClearAutocomplete();
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Error updating autocomplete state");
            context.InputContext.ClearAutocomplete();
        }
    }

    private void NavigateAutocomplete(ITuiContext context, bool up)
    {
        if (!context.InputContext.ShowSuggestions || context.InputContext.Suggestions.Count == 0)
        {
            return;
        }

        if (up)
        {
            context.InputContext.SelectedSuggestionIndex =
                (context.InputContext.SelectedSuggestionIndex - 1 + context.InputContext.Suggestions.Count)
                % context.InputContext.Suggestions.Count;
        }
        else
        {
            context.InputContext.SelectedSuggestionIndex =
                (context.InputContext.SelectedSuggestionIndex + 1)
                % context.InputContext.Suggestions.Count;
        }
    }

    private void AcceptAutocompleteSuggestion(ITuiContext context)
    {
        context.AutocompleteManager.AcceptSuggestion(context.InputContext);
    }

    private void CancelAutocomplete(ITuiContext context)
    {
        context.InputContext.ClearAutocomplete();
    }

    private void NavigateUserSelection(ITuiContext context, bool up)
    {
        if (context.InputContext.CompletionItems.Count == 0)
        {
            return;
        }


        if (up)
        {
            context.InputContext.SelectedSuggestionIndex =
                (context.InputContext.SelectedSuggestionIndex - 1 + context.InputContext.CompletionItems.Count)
                % context.InputContext.CompletionItems.Count;
        }
        else
        {
            context.InputContext.SelectedSuggestionIndex =
                (context.InputContext.SelectedSuggestionIndex + 1)
                % context.InputContext.CompletionItems.Count;
        }
    }

}

#pragma warning restore IDE0010 // Add missing cases
#pragma warning restore IDE0045 // Convert to conditional expression
