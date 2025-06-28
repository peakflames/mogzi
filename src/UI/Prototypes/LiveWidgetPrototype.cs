namespace UI.Prototypes;

/// <summary>
/// Simple prototype demonstrating Live widget capabilities for real-time UI updates.
/// This prototype shows how we can achieve Gemini-CLI-like real-time updates using Spectre.Console Live widgets.
/// </summary>
public static class LiveWidgetPrototype
{
    /// <summary>
    /// Runs a simple Live widget demonstration showing real-time updates.
    /// </summary>
    public static async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var console = AnsiConsole.Console;
        console.Clear();
        
        // Create the main layout that will be updated in real-time
        var layout = new Spectre.Console.Layout("Root")
            .SplitRows(
                new Spectre.Console.Layout("Header").Size(3),
                new Spectre.Console.Layout("Content").SplitColumns(
                    new Spectre.Console.Layout("Input").Size(40),
                    new Spectre.Console.Layout("Output")
                ),
                new Spectre.Console.Layout("Footer").Size(2)
            );

        // Initialize layout content
        UpdateHeader(layout);
        UpdateInput(layout, "");
        UpdateOutput(layout, "Welcome to Live Widget Prototype!");
        UpdateFooter(layout);

        // Start the Live widget - this is the key difference from polling approach
        await console.Live(layout)
            .StartAsync(async ctx =>
            {
                // Simulate real-time updates like Gemini-CLI
                await DemonstrateRealTimeUpdates(layout, ctx, cancellationToken);
            });
    }

    /// <summary>
    /// Demonstrates various real-time update scenarios.
    /// </summary>
    private static async Task DemonstrateRealTimeUpdates(Spectre.Console.Layout layout, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        // Demo 1: Animated loading indicator (like Gemini-CLI's spinner)
        await DemoLoadingIndicator(layout, ctx, cancellationToken);
        
        // Demo 2: Streaming text updates (like AI response streaming)
        await DemoStreamingText(layout, ctx, cancellationToken);
        
        // Demo 3: Real-time input simulation (like typing)
        await DemoRealTimeInput(layout, ctx, cancellationToken);
        
        // Demo 4: Tool execution visualization
        await DemoToolExecution(layout, ctx, cancellationToken);
        
        // Final message
        UpdateOutput(layout, "[green]✅ Live widget prototype completed successfully![/]\n\nThis demonstrates the foundation for Gemini-CLI-like UX.");
        ctx.Refresh();
        
        // Wait for user to see final result
        await Task.Delay(3000, cancellationToken);
    }

    /// <summary>
    /// Demo 1: Animated loading indicator with elapsed time (like Gemini-CLI).
    /// </summary>
    private static async Task DemoLoadingIndicator(Spectre.Console.Layout layout, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        UpdateOutput(layout, "[yellow]Demo 1: Animated Loading Indicator[/]");
        ctx.Refresh();
        await Task.Delay(1000, cancellationToken);

        var spinnerFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        var startTime = DateTime.Now;
        
        for (int i = 0; i < 30 && !cancellationToken.IsCancellationRequested; i++)
        {
            var elapsed = DateTime.Now - startTime;
            var spinner = spinnerFrames[i % spinnerFrames.Length];
            var message = $"[yellow]{spinner}[/] [purple]AI is thinking...[/] [dim]({elapsed.TotalSeconds:F1}s)[/]";
            
            UpdateOutput(layout, $"[yellow]Demo 1: Animated Loading Indicator[/]\n\n{message}");
            ctx.Refresh();
            
            await Task.Delay(200, cancellationToken);
        }
    }

    /// <summary>
    /// Demo 2: Streaming text updates (like AI response streaming).
    /// </summary>
    private static async Task DemoStreamingText(Spectre.Console.Layout layout, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        UpdateOutput(layout, "[blue]Demo 2: Streaming Text Updates[/]");
        ctx.Refresh();
        await Task.Delay(1000, cancellationToken);

        var streamingText = "This demonstrates real-time streaming text updates, similar to how Gemini-CLI displays AI responses as they arrive. Each character appears smoothly without screen flicker.";
        var displayedText = "";
        
        foreach (char c in streamingText)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            displayedText += c;
            UpdateOutput(layout, $"[blue]Demo 2: Streaming Text Updates[/]\n\n{displayedText}[blink]▋[/]");
            ctx.Refresh();
            
            await Task.Delay(50, cancellationToken);
        }
        
        // Remove cursor
        UpdateOutput(layout, $"[blue]Demo 2: Streaming Text Updates[/]\n\n{displayedText}");
        ctx.Refresh();
        await Task.Delay(1500, cancellationToken);
    }

    /// <summary>
    /// Demo 3: Real-time input simulation (like typing).
    /// </summary>
    private static async Task DemoRealTimeInput(Spectre.Console.Layout layout, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        UpdateOutput(layout, "[green]Demo 3: Real-time Input Updates[/]");
        ctx.Refresh();
        await Task.Delay(1000, cancellationToken);

        var inputText = "How can I improve my code?";
        var currentInput = "";
        
        foreach (char c in inputText)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            currentInput += c;
            UpdateInput(layout, currentInput);
            UpdateOutput(layout, "[green]Demo 3: Real-time Input Updates[/]\n\nWatch the input panel update in real-time as text is typed...");
            ctx.Refresh();
            
            await Task.Delay(150, cancellationToken);
        }
        
        await Task.Delay(1000, cancellationToken);
        
        // Simulate Enter press
        UpdateInput(layout, "");
        UpdateOutput(layout, "[green]Demo 3: Real-time Input Updates[/]\n\n✅ Input submitted: \"How can I improve my code?\"");
        ctx.Refresh();
        await Task.Delay(1500, cancellationToken);
    }

    /// <summary>
    /// Demo 4: Tool execution visualization (like Gemini-CLI's tool calls).
    /// </summary>
    private static async Task DemoToolExecution(Spectre.Console.Layout layout, LiveDisplayContext ctx, CancellationToken cancellationToken)
    {
        UpdateOutput(layout, "[magenta]Demo 4: Tool Execution Visualization[/]");
        ctx.Refresh();
        await Task.Delay(1000, cancellationToken);

        var tools = new[]
        {
            ("read_file", "Reading project structure..."),
            ("search_files", "Searching for code patterns..."),
            ("generate_patch", "Generating improvements...")
        };

        var toolResults = "";
        
        foreach (var (toolName, description) in tools)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            // Show tool starting
            toolResults += $"🔧 [yellow]{toolName}[/]: {description}\n";
            UpdateOutput(layout, $"[magenta]Demo 4: Tool Execution Visualization[/]\n\n{toolResults}");
            ctx.Refresh();
            
            // Simulate tool execution time
            await Task.Delay(1500, cancellationToken);
            
            // Show tool completed
            toolResults = toolResults.Replace($"🔧 [yellow]{toolName}[/]", $"✅ [green]{toolName}[/]");
            UpdateOutput(layout, $"[magenta]Demo 4: Tool Execution Visualization[/]\n\n{toolResults}");
            ctx.Refresh();
            
            await Task.Delay(500, cancellationToken);
        }
        
        await Task.Delay(1500, cancellationToken);
    }

    /// <summary>
    /// Updates the header section of the layout.
    /// </summary>
    private static void UpdateHeader(Spectre.Console.Layout layout)
    {
        var header = new Panel(
            new Markup("[bold blue]MaxBot Live Widget Prototype[/] [dim]- Real-time UI Updates[/]"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
            
        layout["Header"].Update(header);
    }

    /// <summary>
    /// Updates the input section of the layout.
    /// </summary>
    private static void UpdateInput(Spectre.Console.Layout layout, string inputText)
    {
        var prompt = "[blue]>[/] ";
        var cursor = string.IsNullOrEmpty(inputText) ? "[blink]▋[/]" : "";
        var content = $"{prompt}{inputText}{cursor}";
        
        var inputPanel = new Panel(new Markup(content))
            .Header("Input")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green);
            
        layout["Input"].Update(inputPanel);
    }

    /// <summary>
    /// Updates the output section of the layout.
    /// </summary>
    private static void UpdateOutput(Spectre.Console.Layout layout, string content)
    {
        var outputPanel = new Panel(new Markup(content))
            .Header("Output")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
            
        layout["Output"].Update(outputPanel);
    }

    /// <summary>
    /// Updates the footer section of the layout.
    /// </summary>
    private static void UpdateFooter(Spectre.Console.Layout layout)
    {
        var footer = new Panel(
            new Markup("[dim]Press Ctrl+C to exit | Live widgets enable real-time updates without polling[/]"))
            .Border(BoxBorder.None);
            
        layout["Footer"].Update(footer);
    }
}
