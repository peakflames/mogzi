namespace UI.Prototypes;

/// <summary>
/// Prototype demonstrating Gemini-CLI-like scrolling chat history with dynamic bottom component.
/// This shows the correct architecture: scrollable history above + state-based bottom area.
/// </summary>
public static class ScrollingChatPrototype
{
    /// <summary>
    /// Runs the scrolling chat prototype demonstration.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var console = AnsiConsole.Console;
        console.Clear();
        
        // Initialize the chat state
        var chatHistory = new List<ChatMessage>();
        var currentState = ChatState.Input;
        var currentInput = "";
        var toolProgress = "";
        
        // Add initial welcome messages to history
        chatHistory.Add(new ChatMessage(MessageType.System, "Tips for getting started:"));
        chatHistory.Add(new ChatMessage(MessageType.System, "1. Ask questions, edit files, or run commands."));
        chatHistory.Add(new ChatMessage(MessageType.System, "2. Be specific for the best results."));
        chatHistory.Add(new ChatMessage(MessageType.System, "3. Create GEMINI.md files to customize your interactions with Gemini."));
        chatHistory.Add(new ChatMessage(MessageType.System, "4. /help for more information."));

        // Create the main layout - this will be updated in real-time
        var layout = new Spectre.Console.Layout("Root")
            .SplitRows(
                new Spectre.Console.Layout("History"),  // Scrollable chat history
                new Spectre.Console.Layout("Bottom").Size(4),  // Dynamic bottom area (input/tool execution)
                new Spectre.Console.Layout("Footer").Size(1)   // Status bar
            );

        // Start the Live widget for real-time updates
        await console.Live(layout)
            .StartAsync(async ctx =>
            {
                // Initial render
                UpdateLayout(layout, chatHistory, currentState, currentInput, toolProgress);
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
        await Task.Delay(2000, cancellationToken);
        chatHistory.Add(new ChatMessage(MessageType.System, "[green]✅ Scrolling chat prototype completed![/]"));
        chatHistory.Add(new ChatMessage(MessageType.System, "This demonstrates the Gemini-CLI interaction pattern."));
        UpdateLayout(layout, chatHistory, ChatState.Input, "", "");
        ctx.Refresh();
        
        await Task.Delay(3000, cancellationToken);
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
            UpdateLayout(layout, chatHistory, ChatState.Input, currentInput, "");
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
        chatHistory.Add(new ChatMessage(MessageType.User, 
            "lets make a html and js file that shows off the power of tailwind. Makes sure to bring in tailwind from a CDn etc"));
        
        // Clear input and show thinking state
        UpdateLayout(layout, chatHistory, ChatState.Thinking, "", "");
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
        chatHistory.Add(new ChatMessage(MessageType.Assistant, ""));
        
        // Stream the response character by character
        foreach (char c in aiResponse)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            currentResponse += c;
            // Update the last message (AI response) in history
            chatHistory[chatHistory.Count - 1] = new ChatMessage(MessageType.Assistant, currentResponse);
            
            UpdateLayout(layout, chatHistory, ChatState.Thinking, "", "");
            ctx.Refresh();
            
            await Task.Delay(30, cancellationToken); // Streaming speed
        }
        
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
            ("Building Initial Structure", 5),
            ("Creating HTML File", 3),
            ("Adding Tailwind CDN", 2),
            ("Creating JavaScript File", 3),
            ("Finalizing Project", 2)
        };

        foreach (var (stepName, duration) in toolSteps)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            for (int i = duration; i >= 1; i--)
            {
                var progress = $"{stepName} (esc to cancel, {i}s)";
                UpdateLayout(layout, chatHistory, ChatState.ToolExecution, "", progress);
                ctx.Refresh();
                
                await Task.Delay(1000, cancellationToken);
            }
        }
        
        // Add tool completion message to history
        chatHistory.Add(new ChatMessage(MessageType.System, "[green]✅ Files created successfully![/]"));
        chatHistory.Add(new ChatMessage(MessageType.System, "• index.html - Main HTML file with Tailwind CSS"));
        chatHistory.Add(new ChatMessage(MessageType.System, "• script.js - Interactive JavaScript functionality"));
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
        UpdateLayout(layout, chatHistory, ChatState.Input, "", "");
        ctx.Refresh();
        
        await Task.Delay(1000, cancellationToken);
    }

    /// <summary>
    /// Updates the entire layout based on current state.
    /// </summary>
    private static void UpdateLayout(
        Spectre.Console.Layout layout, 
        List<ChatMessage> chatHistory, 
        ChatState state, 
        string currentInput, 
        string toolProgress)
    {
        // Update scrollable history area
        UpdateHistoryArea(layout, chatHistory);
        
        // Update dynamic bottom area based on state
        UpdateBottomArea(layout, state, currentInput, toolProgress);
        
        // Update footer
        UpdateFooter(layout);
    }

    /// <summary>
    /// Updates the scrollable chat history area.
    /// </summary>
    private static void UpdateHistoryArea(Spectre.Console.Layout layout, List<ChatMessage> chatHistory)
    {
        var historyContent = new List<IRenderable>();
        
        // Add Gemini logo/header
        historyContent.Add(new Markup("[bold blue]🔷 GEMINI[/]"));
        historyContent.Add(new Text(""));
        
        // Add all chat messages
        foreach (var message in chatHistory)
        {
            var prefix = message.Type switch
            {
                MessageType.User => "[dim]>[/] ",
                MessageType.Assistant => "✦ ",
                MessageType.System => "",
                _ => ""
            };
            
            var color = message.Type switch
            {
                MessageType.User => "dim",
                MessageType.Assistant => "white",
                MessageType.System => "dim",
                _ => "white"
            };
            
            if (!string.IsNullOrEmpty(message.Content))
            {
                historyContent.Add(new Markup($"[{color}]{prefix}{message.Content}[/]"));
                historyContent.Add(new Text(""));
            }
        }
        
        var historyPanel = new Panel(new Rows(historyContent))
            .Border(BoxBorder.None)
            .Padding(1, 0);
            
        layout["History"].Update(historyPanel);
    }

    /// <summary>
    /// Updates the dynamic bottom area based on current state.
    /// </summary>
    private static void UpdateBottomArea(Spectre.Console.Layout layout, ChatState state, string currentInput, string toolProgress)
    {
        IRenderable bottomContent = state switch
        {
            ChatState.Input => CreateInputComponent(currentInput),
            ChatState.Thinking => CreateThinkingComponent(),
            ChatState.ToolExecution => CreateToolExecutionComponent(toolProgress),
            _ => CreateInputComponent(currentInput)
        };
        
        layout["Bottom"].Update(bottomContent);
    }

    /// <summary>
    /// Creates the input component.
    /// </summary>
    private static IRenderable CreateInputComponent(string currentInput)
    {
        var prompt = "[blue]>[/] ";
        var cursor = "[blink]▋[/]";
        var content = $"{prompt}{currentInput}{cursor}";
        
        return new Panel(new Markup(content))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Header("Type your message or @path/to/file");
    }

    /// <summary>
    /// Creates the thinking/processing component.
    /// </summary>
    private static IRenderable CreateThinkingComponent()
    {
        return new Panel(new Markup("[yellow]⚡[/] [dim]AI is thinking...[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
    }

    /// <summary>
    /// Creates the tool execution component.
    /// </summary>
    private static IRenderable CreateToolExecutionComponent(string progress)
    {
        return new Panel(new Markup($"[yellow]⚡[/] {progress}"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
    }

    /// <summary>
    /// Updates the footer/status bar.
    /// </summary>
    private static void UpdateFooter(Spectre.Console.Layout layout)
    {
        var footer = new Markup("[dim]~/Desktop/maxbot-demo    no sandbox (see /docs)    gemini-2.5-pro (100% context left)[/]");
        layout["Footer"].Update(footer);
    }
}

/// <summary>
/// Represents a chat message in the conversation history.
/// </summary>
public record ChatMessage(MessageType Type, string Content);

/// <summary>
/// Types of messages in the chat.
/// </summary>
public enum MessageType
{
    User,
    Assistant,
    System
}

/// <summary>
/// Current state of the chat interface.
/// </summary>
public enum ChatState
{
    Input,
    Thinking,
    ToolExecution
}
