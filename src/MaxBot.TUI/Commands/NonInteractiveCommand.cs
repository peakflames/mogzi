using MaxBot.Domain;
using MaxBot.TUI.Infrastructure;

namespace MaxBot.TUI.Commands;

/// <summary>
/// Non-interactive command for AI queries without TUI.
/// </summary>
public sealed class NonInteractiveCommand : ICommand
{
    public string Name => "run";
    public string Description => "Run a single prompt non-interactively";

    public async Task<int> ExecuteAsync(string[] args)
    {
        try
        {
            var parsedArgs = ArgumentParser.Parse(args);
            
            // Check for help
            if (ArgumentParser.HasFlag(parsedArgs, "help") || ArgumentParser.HasFlag(parsedArgs, "h"))
            {
                ShowHelp();
                return 0;
            }
            
            // Get prompt from arguments
            var prompt = ArgumentParser.GetString(parsedArgs, ["prompt", "p"], null);
            
            // Check for STDIN input and handle it like the CLI version
            if (Console.IsInputRedirected)
            {
                var stdinInput = await Console.In.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(stdinInput))
                {
                    if (!string.IsNullOrWhiteSpace(prompt))
                    {
                        // Concatenate STDIN input with prompt argument (STDIN first, then prompt)
                        prompt = stdinInput.Trim() + Environment.NewLine + prompt;
                    }
                    else
                    {
                        // Use STDIN input as the prompt
                        prompt = stdinInput.Trim();
                    }
                }
            }
            
            if (string.IsNullOrWhiteSpace(prompt))
            {
                AnsiConsole.MarkupLine("[red]Error: Prompt is required for non-interactive mode. Use --prompt or -p to specify the message, path to a .md file, or pipe input via STDIN.[/]");
                return 1;
            }
            
            // Extract configuration parameters
            var configPath = ArgumentParser.GetString(parsedArgs, ["config"], null);
            var profileName = ArgumentParser.GetString(parsedArgs, ["profile"], null);
            var toolApprovals = ArgumentParser.GetString(parsedArgs, ["tool-approvals", "ta"], null);
            
            // Validate tool-approvals value if provided
            if (!string.IsNullOrEmpty(toolApprovals) && 
                toolApprovals != "readonly" && toolApprovals != "all")
            {
                AnsiConsole.MarkupLine("[red]Error: --tool-approvals must be either 'readonly' or 'all'[/]");
                return 1;
            }
            
            // Setup dependency injection
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services, configPath, profileName, toolApprovals);
            var serviceProvider = services.BuildServiceProvider();
            
            var appService = serviceProvider.GetRequiredService<IAppService>();
            var historyManager = serviceProvider.GetRequiredService<HistoryManager>();
            var console = serviceProvider.GetRequiredService<IAnsiConsole>();

            // Resolve the prompt (either from string or file)
            var promptText = await ResolvePromptAsync(prompt, console);
            if (string.IsNullOrEmpty(promptText))
            {
                return 1; // Error already displayed
            }

            // Check if we should use history
            var noHistory = ArgumentParser.GetBool(parsedArgs, "no-history");

            // Create chat history
            var chatHistory = new List<ChatMessage>();
            
            if (!noHistory)
            {
                // Load existing history
                var existingHistory = historyManager.GetCurrentChatHistory();
                if (existingHistory.Count > 0)
                {
                    // TODO: Implement once session management is implemented
                    // console.MarkupLine($"[dim]Loading {existingHistory.Count} messages from existing chat history[/]");
                    chatHistory.AddRange(existingHistory);
                }
                else
                {
                    // TODO: Implement once session management is implemented
                    // console.MarkupLine("[dim]No existing chat history found, starting fresh session[/]");
                }
            }
            else
            {
                console.MarkupLine("[dim]--no-history flag used, starting fresh session without loading previous chat history[/]");
            }

            // Add user message
            var userMessage = new ChatMessage(ChatRole.User, promptText);
            chatHistory.Add(userMessage);

            if (!noHistory)
            {
                historyManager.AddUserMessage(userMessage);
            }

            // Process the message and stream output directly to stdout
            var responseText = new StringBuilder();
            var responseStream = appService.ProcessChatMessageAsync(chatHistory, CancellationToken.None);

            await foreach (var responseUpdate in responseStream)
            {
                if (!string.IsNullOrEmpty(responseUpdate.Text))
                {
                    responseText.Append(responseUpdate.Text);
                    console.Write(responseUpdate.Text);
                }
            }

            var finalResponse = responseText.ToString();
            var assistantMessage = new ChatMessage(ChatRole.Assistant, finalResponse);

            if (!noHistory)
            {
                historyManager.AddAssistantMessage(assistantMessage);
            }

            // Add final newline if response doesn't end with one
            if (!string.IsNullOrEmpty(finalResponse) && !finalResponse.EndsWith('\n'))
            {
                console.WriteLine();
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine("Operation cancelled.");
            return 130; // Standard exit code for SIGINT
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    public void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]DESCRIPTION:[/]");
        AnsiConsole.MarkupLine($"    {Description}");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLine("    max run [[OPTIONS]]");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("    -p, --prompt <PROMPT>        The prompt/message to send to the AI, or path to a .md file containing the prompt");
        AnsiConsole.MarkupLine("    -v, --verbosity <LEVEL>      Set the verbosity level (quiet, minimal, normal, detailed, diagnostic)");
        AnsiConsole.MarkupLine("        --config <PATH>          Path to the configuration file (default: maxbot.config.json)");
        AnsiConsole.MarkupLine("        --profile <NAME>         Configuration profile to use");
        AnsiConsole.MarkupLine("    -ta, --tool-approvals <MODE> Override tool approval mode (readonly, all)");
        AnsiConsole.MarkupLine("        --no-history             Don't use or save chat history");
        AnsiConsole.MarkupLine("    -h, --help                   Show this help message");
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("    max run -p \"What is the capital of Michigan?\"");
        AnsiConsole.MarkupLine("    max run -p ./prompts/analyze-code.md");
        AnsiConsole.MarkupLine("    max run --prompt \"Explain this code\" --no-history");
        AnsiConsole.MarkupLine("    max run -p \"Create a file\" --tool-approvals all");
        AnsiConsole.MarkupLine("    max run -p \"List files\" -ta readonly");
    }

    /// <summary>
    /// Resolves the prompt from either a string or a markdown file path.
    /// </summary>
    private static async Task<string?> ResolvePromptAsync(string promptInput, IAnsiConsole console)
    {
        try
        {
            // Check if the input looks like a file path and ends with .md
            if (promptInput.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && 
                (promptInput.Contains('/') || promptInput.Contains('\\') || promptInput.Contains('.')))
            {
                // Treat as potential file path
                var filePath = Path.GetFullPath(promptInput);
                
                if (File.Exists(filePath))
                {
                    console.MarkupLine($"[dim]Reading prompt from: {filePath}[/]");
                    var content = await File.ReadAllTextAsync(filePath);
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        console.MarkupLine($"[red]Error: The file '{filePath}' is empty.[/]");
                        return null;
                    }
                    
                    return content.Trim();
                }
                else
                {
                    console.MarkupLine($"[red]Error: File not found: {filePath}[/]");
                    return null;
                }
            }
            
            // Treat as direct prompt text
            return promptInput;
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error reading prompt: {ex.Message}[/]");
            return null;
        }
    }
}
