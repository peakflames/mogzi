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

            // Create and run the FlexColumn TUI application
            var app = serviceProvider.GetRequiredService<FlexColumnTuiApp>();

            // Convert parsed args back to string array format for compatibility
            var tuiArgs = BuildArgsFromParsed(parsedArgs);

            return await app.RunAsync(tuiArgs);
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
        AnsiConsole.MarkupLine("    -ta, --tool-approvals <MODE> Override tool approval mode (readonly, all)");
        AnsiConsole.MarkupLine("    -h, --help                   Show this help message");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("   mogzi chat");
        AnsiConsole.MarkupLine("   mogzi chat --verbosity normal");
        AnsiConsole.MarkupLine("   mogzi chat --profile development");
        AnsiConsole.MarkupLine("   mogzi chat --tool-approvals all");
        AnsiConsole.MarkupLine("   mogzi chat -ta readonly");
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
        if (!string.IsNullOrEmpty(config) && config != "maxbot.config.json")
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

        return [.. args];
    }
}
