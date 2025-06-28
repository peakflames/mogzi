namespace UI.Prototypes;

/// <summary>
/// Prototype demonstrating the correct Gemini-CLI layout pattern:
/// flex flex-col space-y-2 - vertical stack where input is NOT pinned to bottom
/// but naturally flows as the last item in the column.
/// </summary>
public static class FlexColumnPrototype
{
    /// <summary>
    /// Runs the flex column layout prototype demonstration.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var console = AnsiConsole.Console;
        console.Clear();
        
        // Initialize the chat state
        var chatHistory = new List<ChatMessage>();
        var currentState = UI.Components.ChatState.Input;
        var currentInput = "";
        var toolProgress = "";
        
        // Add initial welcome messages to history
        chatHistory.Add(new ChatMessage(ChatRole.System, "Max"));
        chatHistory.Add(new ChatMessage(ChatRole.System, ""));
        chatHistory.Add(new ChatMessage(ChatRole.System, "Tips for getting started:"));
        chatHistory.Add(new ChatMessage(ChatRole.System, "1. Ask questions, edit files, or run commands."));
        chatHistory.Add(new ChatMessage(ChatRole.System, "2. Be specific for the best results."));
        chatHistory.Add(new ChatMessage(ChatRole.System, "3. Create an .max/AGENT.md files to customize your interactions with Max."));
        chatHistory.Add(new ChatMessage(ChatRole.System, "4. /help for more information."));

        // Create a single column layout that flows naturally (like flex-col)
        var layout = new Spectre.Console.Layout("Root")
            .SplitRows(
                new Spectre.Console.Layout("MainContent")
            );

        // Start the Live widget for real-time updates
        await console.Live(layout)
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                // Initial render
                UpdateFlexColumnLayout(layout, chatHistory, currentState, currentInput, toolProgress);
                ctx.Refresh();

                // Simulate the interaction flow
                await SimulateGeminiCliInteraction(layout, chatHistory, ctx, cancellationToken);
            });
    }

    /// <summary>
    /// Simulates a complete Gemini-CLI interaction flow.
    /// </summary>
    private static async Task SimulateGeminiCliInteraction(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        LiveDisplayContext ctx, 
        CancellationToken cancellationToken)
    {
        // Phase 1: User typing a prompt
        await SimulateUserTyping(layout, chatHistory, ctx, 
            "lets make a html and js file that shows off the power of tailwind. Makes sure to bring in tailwind from a CDn etc", 
            cancellationToken);

        // Phase 2: User submits prompt (Enter pressed)
        await SimulatePromptSubmission(layout, chatHistory, ctx, cancellationToken);

        // Phase 3: AI thinking/responding
        await SimulateAiResponse(layout, chatHistory, ctx, cancellationToken);

        // Phase 4: Tool execution
        await SimulateToolExecution(layout, chatHistory, ctx, cancellationToken);

        // Phase 5: Return to input state
        await SimulateReturnToInput(layout, chatHistory, ctx, cancellationToken);

        // Final message
        await Task.Delay(1000, cancellationToken);
        chatHistory.Add(new ChatMessage(ChatRole.System, ""));
        chatHistory.Add(new ChatMessage(ChatRole.System, "[green]✅ Flex column prototype completed![/]"));
        chatHistory.Add(new ChatMessage(ChatRole.System, "This demonstrates the correct Gemini-CLI layout pattern."));
        UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.Input, "", "");
        ctx.Refresh();
        
        await Task.Delay(1000, cancellationToken);
    }

    /// <summary>
    /// Simulates user typing in the input field.
    /// </summary>
    private static async Task SimulateUserTyping(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        LiveDisplayContext ctx, 
        string message, 
        CancellationToken cancellationToken)
    {
        var currentInput = "";
        
        foreach (char c in message)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            currentInput += c;
            UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.Input, currentInput, "");
            ctx.Refresh();
            
            await Task.Delay(50, cancellationToken); // Typing speed
        }
        
        await Task.Delay(1000, cancellationToken); // Pause before submission
    }

    /// <summary>
    /// Simulates prompt submission (Enter key pressed).
    /// </summary>
    private static async Task SimulatePromptSubmission(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        LiveDisplayContext ctx, 
        CancellationToken cancellationToken)
    {
        // Add user message to history
        chatHistory.Add(new ChatMessage(ChatRole.User, 
            "lets make a html and js file that shows off the power of tailwind. Makes sure to bring in tailwind from a CDn etc"));
        
        // Add empty line for spacing
        chatHistory.Add(new ChatMessage(ChatRole.System, ""));
        
        // Clear input and show thinking state
        UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.Thinking, "", "");
        ctx.Refresh();
        
        await Task.Delay(500, cancellationToken);
    }

    /// <summary>
    /// Simulates AI response streaming.
    /// </summary>
    private static async Task SimulateAiResponse(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        LiveDisplayContext ctx, 
        CancellationToken cancellationToken)
    {
        var aiResponse = "I will create an index.html file that includes the Tailwind CSS CDN and a script.js file for interactive elements, demonstrating a card with responsive text and a button that changes styles on click.";
        var currentResponse = "";
        
        // Add empty AI message to history
        chatHistory.Add(new ChatMessage(ChatRole.Assistant, ""));
        
        // Stream the response character by character
        foreach (char c in aiResponse)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            currentResponse += c;
            // Update the last message (AI response) in history
            chatHistory[chatHistory.Count - 1] = new ChatMessage(ChatRole.Assistant, currentResponse);
            
            UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.Thinking, "", "");
            ctx.Refresh();
            
            await Task.Delay(30, cancellationToken); // Streaming speed
        }
        
        // Add spacing after AI response
        chatHistory.Add(new ChatMessage(ChatRole.System, ""));
        
        await Task.Delay(1000, cancellationToken);
    }

    /// <summary>
    /// Simulates tool execution with progress updates.
    /// </summary>
    private static async Task SimulateToolExecution(
        Spectre.Console.Layout layout,
        List<ChatMessage> chatHistory,
        LiveDisplayContext ctx,
        CancellationToken cancellationToken)
    {
        var toolSteps = new[]
        {
            ("Building Initial Structure", 5000),
            ("Creating HTML File", 3000),
            ("Adding Tailwind CDN", 2000),
            ("Creating JavaScript File", 3000),
            ("Finalizing Project", 2000)
        };

        var updateDelay = 100;

        foreach (var (stepName, duration) in toolSteps)
        {
            if (cancellationToken.IsCancellationRequested) break;

            for (int i = 0; i < duration; i += updateDelay)
            {
                var progress = $"{stepName} (esc to cancel, {TimeSpan.FromMilliseconds(i).TotalSeconds}s)";
                UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.ToolExecution, "", progress);
                ctx.Refresh();

                await Task.Delay(updateDelay, cancellationToken);
            }
        }

        // Add tool completion message to history
        chatHistory.Add(new ChatMessage(ChatRole.System, "[green]✅ Files created successfully![/]"));
        chatHistory.Add(new ChatMessage(ChatRole.System, "• index.html - Main HTML file with Tailwind CSS"));
        chatHistory.Add(new ChatMessage(ChatRole.System, "• script.js - Interactive JavaScript functionality"));
        chatHistory.Add(new ChatMessage(ChatRole.System, ""));
    }

    /// <summary>
    /// Simulates returning to input state after tool completion.
    /// </summary>
    private static async Task SimulateReturnToInput(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        LiveDisplayContext ctx, 
        CancellationToken cancellationToken)
    {
        UpdateFlexColumnLayout(layout, chatHistory, UI.Components.ChatState.Input, "", "");
        ctx.Refresh();
        
        await Task.Delay(1000, cancellationToken);
    }

    /// <summary>
    /// Converts ChatRole to MessageType for UI rendering.
    /// </summary>
    private static UI.Components.MessageType GetMessageType(ChatMessage message)
    {
        if (message.Role == ChatRole.User)
            return UI.Components.MessageType.User;
        else if (message.Role == ChatRole.Assistant)
            return UI.Components.MessageType.Assistant;
        else if (message.Role == ChatRole.System)
            return UI.Components.MessageType.System;
        else
            return UI.Components.MessageType.System;
    }

    /// <summary>
    /// Updates the flex column layout - everything flows in a single column like flex-col.
    /// </summary>
    private static void UpdateFlexColumnLayout(
        Spectre.Console.Layout layout,
        List<ChatMessage> chatHistory,
        UI.Components.ChatState state,
        string currentInput,
        string toolProgress)
    {
        // Create all content as a single flowing column
        var contentItems = new List<IRenderable>();

        UI.Components.MessageType? prevMessageType = null;

        // Add all chat history with more vertical spacing
        foreach (var message in chatHistory)
        {
            if (!string.IsNullOrEmpty(message.Text))
            {
                var messageType = GetMessageType(message);
                
                // Add extra spacing after when messages types change
                if (prevMessageType != messageType)
                {
                    contentItems.Add(new Text(""));
                }

                var prefix = messageType switch
                {
                    UI.Components.MessageType.User => "[dim]>[/] ",
                    UI.Components.MessageType.Assistant => "✦ ",
                    UI.Components.MessageType.System => "",
                    _ => ""
                };

                var color = messageType switch
                {
                    UI.Components.MessageType.User => "dim",
                    UI.Components.MessageType.Assistant => "white",
                    UI.Components.MessageType.System => "grey69",
                    _ => "white"
                };

                contentItems.Add(new Markup($"[{color}]{prefix}{message.Text}[/]"));

                // Add extra spacing after when messages types change
                if (prevMessageType != messageType)
                {
                    contentItems.Add(new Text(""));
                }

                prevMessageType = messageType;
            }
        }


        // Add extra top spacing before the bottom component
        contentItems.Add(new Text(""));

        // Add current state component at the bottom (like the last item in flex-col)
        var bottomComponent = state switch
        {
            UI.Components.ChatState.Input => CreateFlexInputComponent(currentInput),
            UI.Components.ChatState.Thinking => CreateFlexThinkingComponent(),
            UI.Components.ChatState.ToolExecution => CreateFlexToolExecutionComponent(toolProgress),
            _ => CreateFlexInputComponent(currentInput)
        };

        contentItems.Add(bottomComponent);

        // Add extra top spacing before the bottom component
        contentItems.Add(new Text(""));

        // Add current footer
        var footerComponent = CreateFlexFooterComponent();
        contentItems.Add(footerComponent);

        // Create the dynamic zone content panel with all items flowing vertically
        var contentPanel = new Panel(new Rows(contentItems))
            .Padding(1, 0)
            .NoBorder();

        layout["MainContent"].Update(contentPanel);

    }

    /// <summary>
    /// Creates the input component for flex layout.
    /// </summary>
    private static IRenderable CreateFlexInputComponent(string currentInput)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var placeholder = string.IsNullOrEmpty(currentInput) ? "[dim]Type your message or @path/to/file[/]" : "";
        var content = $"{prompt}{currentInput}{cursor}";

        return new Panel(new Markup(content))
                    .NoBorder();           
    }

    /// <summary>
    /// Creates the thinking/processing component for flex layout.
    /// </summary>
    private static IRenderable CreateFlexThinkingComponent()
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
    private static IRenderable CreateFlexToolExecutionComponent(string progress)
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
    /// Updates the footer/status bar.
    /// </summary>
    private static IRenderable CreateFlexFooterComponent()
    {
        var content = "[dim]~/Desktop/maxbot-demo    no sandbox (see /docs)    gemini-2.5-pro (100% context left)[/]";
        return new Panel(new Markup(content))
            .NoBorder();
    }
}
