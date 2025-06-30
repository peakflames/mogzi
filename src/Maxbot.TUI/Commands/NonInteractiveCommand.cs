using Spectre.Console.Cli;
using MaxBot.Domain;
using MaxBot.TUI.Infrastructure;
using System.ComponentModel;

namespace MaxBot.TUI.Commands;

/// <summary>
/// Settings for the non-interactive command.
/// </summary>
public sealed class NonInteractiveSettings : CommandSettings
{
    [CommandOption("-p|--prompt <PROMPT>")]
    [Description("The prompt/message to send to the AI, or path to a .md file containing the prompt")]
    public string? Prompt { get; init; }

    [CommandOption("-v|--verbosity <LEVEL>")]
    [Description("Set the verbosity level (quiet, minimal, normal, detailed, diagnostic)")]
    [DefaultValue("quiet")]
    public string Verbosity { get; init; } = "quiet";

    [CommandOption("--config <PATH>")]
    [Description("Path to the configuration file")]
    [DefaultValue("maxbot.config.json")]
    public string ConfigPath { get; init; } = "maxbot.config.json";

    [CommandOption("--profile <NAME>")]
    [Description("Configuration profile to use")]
    public string? Profile { get; init; }

    [CommandOption("--no-history")]
    [Description("Don't use or save chat history")]
    [DefaultValue(false)]
    public bool NoHistory { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Prompt))
        {
            return ValidationResult.Error("Prompt is required for non-interactive mode. Use --prompt or -p to specify the message or path to a .md file.");
        }

        return ValidationResult.Success();
    }
}

/// <summary>
/// Non-interactive command for AI queries without TUI.
/// </summary>
public sealed class NonInteractiveCommand : AsyncCommand<NonInteractiveSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, NonInteractiveSettings settings)
    {
        try
        {
            // Setup dependency injection
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();
            
            var appService = serviceProvider.GetRequiredService<IAppService>();
            var historyManager = serviceProvider.GetRequiredService<HistoryManager>();
            var console = serviceProvider.GetRequiredService<IAnsiConsole>();

            // Resolve the prompt (either from string or file)
            var promptText = await ResolvePromptAsync(settings.Prompt!, console);
            if (string.IsNullOrEmpty(promptText))
            {
                return 1; // Error already displayed
            }

            // Create chat history
            var chatHistory = new List<ChatMessage>();
            
            if (!settings.NoHistory)
            {
                // Load existing history
                chatHistory.AddRange(historyManager.GetCurrentChatHistory());
            }

            // Add user message
            var userMessage = new ChatMessage(ChatRole.User, promptText);
            chatHistory.Add(userMessage);

            if (!settings.NoHistory)
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

            if (!settings.NoHistory)
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
