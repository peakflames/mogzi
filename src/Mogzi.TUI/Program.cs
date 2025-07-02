namespace Mogzi.TUI;

/// <summary>
/// Entry point for the UI application.
/// </summary>
public static class Program
{
    private static readonly ICommand[] Commands =
    [
        new ChatCommand(),
        new NonInteractiveCommand()
    ];

    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse arguments to get command name
            var parsedArgs = ArgumentParser.Parse(args);
            var commandName = ArgumentParser.GetString(parsedArgs, "_0");

            // Handle global flags only when no command is specified
            if (string.IsNullOrEmpty(commandName))
            {
                if (ArgumentParser.HasFlag(parsedArgs, "help") || ArgumentParser.HasFlag(parsedArgs, "h"))
                {
                    ShowGlobalHelp();
                    return 0;
                }

                if (ArgumentParser.HasFlag(parsedArgs, "version") || ArgumentParser.HasFlag(parsedArgs, "v"))
                {
                    AnsiConsole.WriteLine(GetApplicationVersion());
                    return 0;
                }

                if (ArgumentParser.HasFlag(parsedArgs, "list-profiles") || ArgumentParser.HasFlag(parsedArgs, "lp"))
                {
                    ListProfiles();
                    return 0;
                }

                // Default to chat if no command specified
                commandName = "chat";
            }

            // Find and execute command
            var command = Commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (command == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Unknown command '{commandName}'[/]");
                AnsiConsole.WriteLine();
                ShowGlobalHelp();
                return 1;
            }

            // Remove command name from args and pass the rest to the command
            var commandArgs = args.Length > 0 && !string.IsNullOrEmpty(ArgumentParser.GetString(parsedArgs, "_0"))
                ? [.. args.Skip(1)]
                : args;
            return await command.ExecuteAsync(commandArgs);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    /// <summary>
    /// Shows global help information.
    /// </summary>
    private static void ShowGlobalHelp()
    {
        AnsiConsole.MarkupLine($"[bold]mogzi[/] [dim]v{GetApplicationVersion()}[/]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]USAGE:[/]");
        AnsiConsole.MarkupLine("    mogzi [[COMMAND]] [[OPTIONS]]");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]COMMANDS:[/]");
        foreach (var command in Commands)
        {
            AnsiConsole.MarkupLine($"    [cyan]{command.Name,-12}[/] {command.Description}");
        }
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]GLOBAL OPTIONS:[/]");
        AnsiConsole.MarkupLine("    -h, --help                   Show this help message");
        AnsiConsole.MarkupLine("    -v, --version                Show version information");
        AnsiConsole.MarkupLine("    -lp, --list-profiles         List all available profiles from the configuration");
        AnsiConsole.MarkupLine("    -ta, --tool-approvals <MODE> Override tool approval mode (readonly, all)");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("[bold]EXAMPLES:[/]");
        AnsiConsole.MarkupLine("    mogzi                                          # Start interactive chat (default)");
        AnsiConsole.MarkupLine("    mogzi chat --verbosity normal                 # Start chat with verbose logging");
        AnsiConsole.MarkupLine("    mogzi chat --tool-approvals all               # Start chat with all tools enabled");
        AnsiConsole.MarkupLine("    mogzi run --prompt \"Hello world\"              # Run single prompt");
        AnsiConsole.MarkupLine("    mogzi run --prompt ./my-prompt.md             # Run prompt from file");
        AnsiConsole.MarkupLine("    mogzi run -p \"Create a file\" -ta all          # Run with all tools enabled");
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("Use '[cyan]mogzi <command> --help[/]' for more information about a specific command.");
    }

    /// <summary>
    /// Gets the application version from the assembly.
    /// </summary>
    /// <returns>The application version string.</returns>
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "UNKNOWN";
        }
        catch
        {
            return "UNKNOWN";
        }
    }

    private static void ListProfiles()
    {
        try
        {
            var configPath = Mogzi.Utils.ConfigurationLocator.FindConfigPath();
            if (configPath is null)
            {
                AnsiConsole.MarkupLine("[red]Error: Could not find mogzi.config.json in the current directory or home directory.[/]");
                return;
            }

            var jsonContent = File.ReadAllText(configPath);
            var configRoot = JsonSerializer.Deserialize(jsonContent, ApplicationConfigurationContext.Default.ApplicationConfigurationRoot);

            var config = configRoot?.MaxbotConfig;
            if (config?.Profiles == null || !config.Profiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No profiles found in mogzi.config.json.[/]");
                return;
            }

            var table = new Table()
                .Title("Available Profiles")
                .Border(TableBorder.Rounded)
                .AddColumn("Name")
                .AddColumn("Model")
                .AddColumn("Provider")
                .AddColumn("Default");

            foreach (var profile in config.Profiles)
            {
                _ = table.AddRow(
                    Markup.Escape(profile.Name ?? "-"),
                    Markup.Escape(profile.ModelId ?? "-"),
                    Markup.Escape(profile.ApiProvider ?? "-"),
                    profile.Default ? ":check_mark_button:" : ""
                );
            }

            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error loading or parsing mogzi.config.json:[/]");
            AnsiConsole.WriteException(ex);
        }
    }
}
