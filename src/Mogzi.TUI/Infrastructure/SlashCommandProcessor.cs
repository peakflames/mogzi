namespace Mogzi.TUI.Infrastructure;

/// <summary>
/// Processes slash commands for the TUI application.
/// Provides essential commands like /help, /clear, /exit, /status.
/// </summary>
public sealed class SlashCommandProcessor
{
    private readonly Dictionary<string, SlashCommand> _commands = [];
    private readonly IAnsiConsole _console;
    private readonly ChatClient? _chatClient;

    /// <summary>
    /// Event raised when the application should exit.
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// Event raised when chat history should be cleared.
    /// </summary>
    public event Action? ClearHistoryRequested;

    /// <summary>
    /// Event raised when an interactive command is requested.
    /// </summary>
    public event Action<string>? InteractiveCommandRequested;

    /// <summary>
    /// Event raised when session history should be cleared.
    /// </summary>
    public event Action? SessionClearRequested;

    /// <summary>
    /// Event raised when session should be renamed.
    /// </summary>
    public event Action<string>? SessionRenameRequested;

    /// <summary>
    /// Initializes a new instance of SlashCommandProcessor.
    /// </summary>
    public SlashCommandProcessor(IAnsiConsole console, ChatClient? chatClient = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _chatClient = chatClient;
        RegisterCommands();
    }

    /// <summary>
    /// Attempts to process a slash command.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="output">The output message from the command, if any.</param>
    /// <returns>True if the input was a slash command and was processed, false otherwise.</returns>
    public bool TryProcessCommand(string input, out string? output)
    {
        output = null;

        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return false;
        }

        var inputLower = input.ToLower();

        // Try to find the longest matching command first (for multi-word commands like "/session clear")
        var matchingCommand = _commands.Keys
            .Where(inputLower.StartsWith)
            .OrderByDescending(cmd => cmd.Length)
            .FirstOrDefault();

        if (matchingCommand != null)
        {
            var cmd = _commands[matchingCommand];
            var args = input.Length > matchingCommand.Length
                ? input[matchingCommand.Length..].Trim()
                : "";

            if (cmd.IsInteractive)
            {
                InteractiveCommandRequested?.Invoke(matchingCommand);
                output = null; // No direct output, handled by TUI
                return true;
            }

            output = cmd.ExecuteWithOutput?.Invoke(args) ?? "Command executed successfully";
            return true;
        }

        // If no exact match found, try single-word command for backward compatibility
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var singleCommand = parts[0].ToLower();

        if (_commands.TryGetValue(singleCommand, out var singleCmd))
        {
            var singleArgs = parts.Length > 1 ? parts[1] : "";

            if (singleCmd.IsInteractive)
            {
                InteractiveCommandRequested?.Invoke(singleCommand);
                output = null; // No direct output, handled by TUI
                return true;
            }

            output = singleCmd.ExecuteWithOutput?.Invoke(singleArgs) ?? "Command executed successfully";
            return true;
        }

        output = GetUnknownCommandMessage(singleCommand);
        return true;
    }

    /// <summary>
    /// Gets command suggestions for autocompletion.
    /// </summary>
    /// <param name="input">The partial input to match against.</param>
    /// <returns>List of matching command names.</returns>
    public List<string> GetCommandSuggestions(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return [];
        }

        var partial = input.ToLower();
        return [.. _commands.Keys
            .Where(cmd => cmd.StartsWith(partial))
            .OrderBy(cmd => cmd)];
    }

    /// <summary>
    /// Gets all available commands for help display.
    /// </summary>
    /// <returns>Dictionary of command names and descriptions.</returns>
    public IReadOnlyDictionary<string, string> GetAllCommands()
    {
        return _commands.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Description);
    }

    /// <summary>
    /// Checks if the given input is a valid slash command.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the input is a valid slash command, false otherwise.</returns>
    public bool IsValidCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return false;
        }

        var inputLower = input.ToLower();

        // Check for multi-word commands first
        var matchingCommand = _commands.Keys
            .Where(inputLower.StartsWith)
            .OrderByDescending(cmd => cmd.Length)
            .FirstOrDefault();

        if (matchingCommand != null)
        {
            return true;
        }

        // Fallback to single-word command check
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();

        return _commands.ContainsKey(command);
    }

    /// <summary>
    /// Checks if the given input is an interactive slash command.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the input is an interactive slash command, false otherwise.</returns>
    public bool IsInteractiveCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return false;
        }

        var inputLower = input.ToLower();

        // Check for multi-word commands first
        var matchingCommand = _commands.Keys
            .Where(inputLower.StartsWith)
            .OrderByDescending(cmd => cmd.Length)
            .FirstOrDefault();

        if (matchingCommand != null)
        {
            return _commands[matchingCommand].IsInteractive;
        }

        // Fallback to single-word command check
        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();

        return _commands.TryGetValue(command, out var cmd) && cmd.IsInteractive;
    }

    /// <summary>
    /// Registers all available slash commands.
    /// </summary>
    private void RegisterCommands()
    {
        _commands["/help"] = new SlashCommand("/help", "Show available commands and usage information", ShowHelp, GetHelpOutput);
        _commands["/clear"] = new SlashCommand("/clear", "Clear the current chat history", ClearHistory, GetClearOutput);
        _commands["/exit"] = new SlashCommand("/exit", "Exit the application gracefully", RequestExit, GetExitOutput);
        _commands["/quit"] = new SlashCommand("/quit", "Exit the application gracefully (alias for /exit)", RequestExit, GetExitOutput);
        _commands["/status"] = new SlashCommand("/status", "Show current system status and information", ShowStatus, GetStatusOutput);
        _commands["/tool-approvals"] = new SlashCommand("/tool-approvals", "Change the tool approval mode for the session", _ => { }, null, true);

        // Session management commands
        _commands["/session clear"] = new SlashCommand("/session clear", "Clear the current session history", ClearSession, GetClearSessionOutput);
        _commands["/session list"] = new SlashCommand("/session list", "List and select from available sessions", _ => { }, null, true);
        _commands["/session rename"] = new SlashCommand("/session rename", "Rename the current session", RenameSession, GetRenameSessionOutput);
    }

    /// <summary>
    /// Shows the help information with all available commands.
    /// </summary>
    private void ShowHelp(string args)
    {
        var table = new Table();
        _ = table.AddColumn(new TableColumn("Command").Centered());
        _ = table.AddColumn("Description");
        table.Border = TableBorder.Rounded;
        _ = table.BorderColor(Color.Blue);

        foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
        {
            _ = table.AddRow(
                new Markup($"[blue]{cmd.Name}[/]"),
                new Markup(cmd.Description)
            );
        }

        var panel = new Panel(table)
            .Header(new PanelHeader(" Available Commands "))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(1, 0);

        _console.Write(panel);
        _console.WriteLine();

        // Add usage tips
        var tips = new Panel(new Rows(
            new Markup("[yellow]Tips:[/]"),
            new Markup("• Type [blue]/[/] and press Tab for command suggestions"),
            new Markup("• Use [blue]Ctrl+C[/] to exit at any time"),
            new Markup("• Use [blue]Ctrl+L[/] to clear the screen")
        ))
        .Header(" Usage Tips ")
        .Border(BoxBorder.Rounded)
        .BorderColor(Color.Yellow)
        .Padding(1, 0);

        _console.Write(tips);
        _console.WriteLine();
    }

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    private void ClearHistory(string args)
    {
        ClearHistoryRequested?.Invoke();

        var panel = new Panel(new Markup("[green]✓[/] Chat history cleared"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        _console.Write(panel);
        _console.WriteLine();
    }

    /// <summary>
    /// Requests application exit.
    /// </summary>
    private void RequestExit(string args)
    {
        var panel = new Panel(new Markup("[yellow]Goodbye![/] Exiting Mogzi..."))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow)
            .Padding(1, 0);

        _console.Write(panel);
        _console.WriteLine();

        ExitRequested?.Invoke();
    }

    /// <summary>
    /// Shows current system status.
    /// </summary>
    private void ShowStatus(string args)
    {
        var statusTable = new Table();
        _ = statusTable.AddColumn("Property");
        _ = statusTable.AddColumn("Value");
        statusTable.Border = TableBorder.Rounded;
        _ = statusTable.BorderColor(Color.Cyan1);

        _ = statusTable.AddRow("Application", "[green]Mogzi TUI[/]");
        _ = statusTable.AddRow("Version", $"[blue]{GetApplicationVersion()}[/]");
        _ = statusTable.AddRow("Status", "[green]Running[/]");
        _ = statusTable.AddRow("Working Directory", $"[dim]{Environment.CurrentDirectory}[/]");
        _ = statusTable.AddRow("Platform", $"[dim]{Environment.OSVersion.Platform}[/]");
        _ = statusTable.AddRow("Runtime", $"[dim].NET {Environment.Version}[/]");

        if (_chatClient != null)
        {
            _ = statusTable.AddRow("", ""); // Empty row for spacing
            _ = statusTable.AddRow("[bold]Configuration[/]", "");
            _ = statusTable.AddRow("Active Profile", $"[yellow]{_chatClient.ActiveProfile.Name}[/]");
            _ = statusTable.AddRow("Model", $"[cyan]{_chatClient.ActiveProfile.ModelId}[/]");
            _ = statusTable.AddRow("API Provider", $"[magenta]{_chatClient.ActiveApiProvider.Name}[/]");
            _ = statusTable.AddRow("Tool Approvals", $"[orange3]{_chatClient.Config.ToolApprovals}[/]");
        }

        var panel = new Panel(statusTable)
            .Header(" System Status ")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .Padding(1, 0);

        _console.Write(panel);
        _console.WriteLine();
    }

    /// <summary>
    /// Gets an error message for unknown commands.
    /// </summary>
    private string GetUnknownCommandMessage(string command)
    {
        return $"Unknown command: {command}\nTip: Type /help to see available commands";
    }

    /// <summary>
    /// Gets help output as text.
    /// </summary>
    private string GetHelpOutput(string args)
    {
        var commands = _commands.Values.OrderBy(c => c.Name).ToList();
        var output = new StringBuilder();

        _ = output.AppendLine("Available Commands:");
        _ = output.AppendLine();

        foreach (var cmd in commands)
        {
            _ = output.AppendLine($"{cmd.Name} - {cmd.Description}");
        }

        _ = output.AppendLine();
        _ = output.AppendLine("Tips:");
        _ = output.AppendLine("• Type / and press Tab for command suggestions");
        _ = output.AppendLine("• Use Ctrl+C to exit at any time");
        _ = output.AppendLine("• Use Ctrl+L to clear the screen");

        return output.ToString();
    }

    /// <summary>
    /// Gets clear output as text.
    /// </summary>
    private string GetClearOutput(string args)
    {
        ClearHistoryRequested?.Invoke();
        return "✓ Chat history cleared";
    }

    /// <summary>
    /// Gets exit output as text.
    /// </summary>
    private string GetExitOutput(string args)
    {
        ExitRequested?.Invoke();
        return "Goodbye! Exiting Mogzi...";
    }

    /// <summary>
    /// Gets status output as text.
    /// </summary>
    private string GetStatusOutput(string args)
    {
        var output = new StringBuilder();
        _ = output.AppendLine("[bold]System Status:[/]");
        _ = output.AppendLine($"[dim]Application:[/] [green]Mogzi TUI[/]");
        _ = output.AppendLine($"[dim]Version:[/] [green]{GetApplicationVersion()}[/]");
        _ = output.AppendLine($"[dim]Status:[/] [green]Running[/]");
        _ = output.AppendLine($"[dim]Working Directory:[/] [green]{Environment.CurrentDirectory}[/]");
        _ = output.AppendLine($"[dim]Platform:[/] [green]{Environment.OSVersion.Platform}[/]");
        _ = output.AppendLine($"[dim]Runtime:[/] [green].NET {Environment.Version}[/]");

        if (_chatClient != null)
        {
            _ = output.AppendLine();
            _ = output.AppendLine("[bold]Configuration:[/]");
            _ = output.AppendLine($"[dim]Active Profile:[/] [cyan]{_chatClient.ActiveProfile.Name}[/]");
            _ = output.AppendLine($"[dim]Model:[/] [cyan]{_chatClient.ActiveProfile.ModelId}[/]");
            _ = output.AppendLine($"[dim]API Provider:[/] [cyan]{_chatClient.ActiveApiProvider.Name}[/]");
            _ = output.AppendLine($"[dim]Tool Approvals:[/] [cyan]{_chatClient.Config.ToolApprovals}[/]");
        }

        return output.ToString();
    }

    /// <summary>
    /// Clears the current session history.
    /// </summary>
    private void ClearSession(string args)
    {
        // This will be handled by the SessionClearEvent
        SessionClearRequested?.Invoke();

        var panel = new Panel(new Markup("[green]✓[/] Session history cleared"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        _console.Write(panel);
        _console.WriteLine();
    }

    /// <summary>
    /// Renames the current session.
    /// </summary>
    private void RenameSession(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var panel = new Panel(new Markup("[red]Error:[/] Session name cannot be empty\nUsage: /session rename <new name>"))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Padding(1, 0);

            _console.Write(panel);
            _console.WriteLine();
            return;
        }

        // This will be handled by the SessionRenameEvent
        SessionRenameRequested?.Invoke(args.Trim());

        var successPanel = new Panel(new Markup($"[green]✓[/] Session renamed to '[cyan]{args.Trim()}[/]'"))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0);

        _console.Write(successPanel);
        _console.WriteLine();
    }

    /// <summary>
    /// Gets clear session output as text.
    /// </summary>
    private string GetClearSessionOutput(string args)
    {
        SessionClearRequested?.Invoke();
        return "✓ Session history cleared";
    }

    /// <summary>
    /// Gets rename session output as text.
    /// </summary>
    private string GetRenameSessionOutput(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return "Error: Session name cannot be empty\nUsage: /session rename <new name>";
        }

        SessionRenameRequested?.Invoke(args.Trim());
        return $"✓ Session renamed to '{args.Trim()}'";
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
}

/// <summary>
/// Represents a slash command with its name, description, and execution logic.
/// </summary>
/// <param name="Name">The command name (including the / prefix).</param>
/// <param name="Description">A description of what the command does.</param>
/// <param name="Execute">The action to execute when the command is invoked.</param>
/// <param name="ExecuteWithOutput">The function to execute when the command is invoked, returning output.</param>
/// <param name="IsInteractive">Whether the command requires interactive handling by the TUI.</param>
public sealed record SlashCommand(string Name, string Description, Action<string> Execute, Func<string, string>? ExecuteWithOutput = null, bool IsInteractive = false);
