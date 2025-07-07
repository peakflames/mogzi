namespace Mogzi.TUI.Commands;

/// <summary>
/// Interactive chat command that launches the TUI.
/// </summary>
public sealed class ChatCommand : ICommand
{
    public string Name => "chat";
    public string Description => "Start interactive chat mode (default)";

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

            // Check for piped input and capture it
            string? pipedInput = null;
            if (Console.IsInputRedirected)
            {
                var stdinInput = await Console.In.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(stdinInput))
                {
                    pipedInput = stdinInput.Trim();
                }
            }

            // Extract configuration parameters
            var configPath = ArgumentParser.GetString(parsedArgs, ["config"], null);
            var profileName = ArgumentParser.GetString(parsedArgs, ["profile"], null);
            var toolApprovals = ArgumentParser.GetString(parsedArgs, ["tool-approvals", "ta"], null);
            var sessionId = ArgumentParser.GetString(parsedArgs, ["session", "s"], null);
            var autoSubmit = ArgumentParser.GetBool(parsedArgs, "auto-submit") || ArgumentParser.GetBool(parsedArgs, "a");

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

            // If we have piped input, set it in the TUI context before starting the app
            if (!string.IsNullOrEmpty(pipedInput))
            {
                var tuiContext = serviceProvider.GetRequiredService<ITuiContext>();
                tuiContext.InputContext.CurrentInput = pipedInput;
                tuiContext.InputContext.CursorPosition = pipedInput.Length;

                // Set auto-submit flag in context if specified
                if (autoSubmit)
                {
                    tuiContext.AutoSubmitPipedInput = true;
                }
            }

            // Create and run the FlexColumn TUI application
            var logger = serviceProvider.GetRequiredService<ILogger<ChatCommand>>();
            var app = serviceProvider.GetRequiredService<FlexColumnTuiApp>();

            // Convert parsed args back to string array format for compatibility
            var tuiArgs = BuildArgsFromParsed(parsedArgs);
            var result = await app.RunAsync(tuiArgs);

            return result;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine("Application cancelled by user.");
            return 0;
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
        AnsiConsole.MarkupLine("   mogzi chat [[OPTIONS]]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]OPTIONS:[/]");
        AnsiConsole.MarkupLine("    -v, --verbosity <LEVEL>      Set the verbosity level (quiet, minimal, normal, detailed, diagnostic)");
        AnsiConsole.MarkupLine("        --config <PATH>          Path to the configuration file (default: mogzi.config.json)");
        AnsiConsole.MarkupLine("        --profile <NAME>         Configuration profile to use");
        AnsiConsole.MarkupLine("    -s, --session <ID_OR_NAME>   Load a specific session by its ID or name");
        AnsiConsole.MarkupLine("    -a, --auto-submit            Automatically submit piped input (default: false)");
        AnsiConsole.MarkupLine("    -ta, --tool-approvals <MODE> Override tool approval mode (readonly, all)");
        AnsiConsole.MarkupLine("    -h, --help                   Show this help message");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("   mogzi chat");
        AnsiConsole.MarkupLine("   mogzi chat --verbosity normal");
        AnsiConsole.MarkupLine("   mogzi chat --profile development");
        AnsiConsole.MarkupLine("   mogzi chat --session 0197e123-4567-7890-abcd-ef1234567890");
        AnsiConsole.MarkupLine("   mogzi chat --session \"My Project Session\"");
        AnsiConsole.MarkupLine("   mogzi chat --tool-approvals all");
        AnsiConsole.MarkupLine("   mogzi chat -ta readonly");
        AnsiConsole.MarkupLine("   echo \"Hello AI\" | mogzi chat");
        AnsiConsole.MarkupLine("   echo \"Hello AI\" | mogzi chat --auto-submit");
    }

    private static string[] BuildArgsFromParsed(Dictionary<string, string?> parsedArgs)
    {
        var args = new List<string>();

        var verbosity = ArgumentParser.GetString(parsedArgs, "verbosity") ??
                       ArgumentParser.GetString(parsedArgs, "v");
        if (!string.IsNullOrEmpty(verbosity) && verbosity != "quiet")
        {
            args.Add("--verbosity");
            args.Add(verbosity);
        }

        var config = ArgumentParser.GetString(parsedArgs, "config");
        if (!string.IsNullOrEmpty(config) && config != "mogzi.config.json")
        {
            args.Add("--config");
            args.Add(config);
        }

        var profile = ArgumentParser.GetString(parsedArgs, "profile");
        if (!string.IsNullOrEmpty(profile))
        {
            args.Add("--profile");
            args.Add(profile);
        }

        var sessionId = ArgumentParser.GetString(parsedArgs, ["session", "s"]);
        if (!string.IsNullOrEmpty(sessionId))
        {
            args.Add("--session");
            args.Add(sessionId);
        }

        return [.. args];
    }
}
