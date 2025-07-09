namespace Mogzi.TUI.Infrastructure;

/// <summary>
/// Processes slash commands for the TUI application.
/// Provides essential commands like /help, /clear, /exit, /status.
/// </summary>
public sealed class SlashCommandProcessor
{
    private readonly Dictionary<string, SlashCommand> _commands = [];
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
    public SlashCommandProcessor(ChatClient? chatClient = null)
    {
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

            // For legacy support, execute component and return simple success message
            if (cmd.ExecuteWithComponent != null)
            {
                _ = cmd.ExecuteWithComponent.Invoke(args);
                output = "Command executed successfully";
                return true;
            }

            output = "Command executed successfully";
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

            // For legacy support, execute component and return simple success message
            if (singleCmd.ExecuteWithComponent != null)
            {
                _ = singleCmd.ExecuteWithComponent.Invoke(singleArgs);
                output = "Command executed successfully";
                return true;
            }

            output = "Command executed successfully";
            return true;
        }

        output = GetUnknownCommandMessage(singleCommand);
        return true;
    }

    /// <summary>
    /// Attempts to process a slash command and return an ITuiComponent.
    /// </summary>
    /// <param name="input">The input string to process.</param>
    /// <param name="component">The ITuiComponent from the command, if any.</param>
    /// <returns>True if the input was a slash command and was processed, false otherwise.</returns>
    public bool TryProcessCommandAsComponent(string input, out ITuiComponent? component)
    {
        component = null;

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
                component = null; // No direct component, handled by TUI
                return true;
            }

            // Try to get component first
            if (cmd.ExecuteWithComponent != null)
            {
                component = cmd.ExecuteWithComponent.Invoke(args);
                return true;
            }

            // No component available for this command
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
                component = null; // No direct component, handled by TUI
                return true;
            }

            // Try to get component first
            if (singleCmd.ExecuteWithComponent != null)
            {
                component = singleCmd.ExecuteWithComponent.Invoke(singleArgs);
                return true;
            }

            // No component available for this command
            return true;
        }

        return false; // Unknown command
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
        _commands["/help"] = new SlashCommand("/help", "Show available commands and usage information", GetHelpComponent);
        _commands["/clear"] = new SlashCommand("/clear", "Clear the current chat history", GetClearComponent);
        _commands["/exit"] = new SlashCommand("/exit", "Exit the application gracefully", GetExitComponent);
        _commands["/quit"] = new SlashCommand("/quit", "Exit the application gracefully (alias for /exit)", GetExitComponent);
        _commands["/status"] = new SlashCommand("/status", "Show current system status and information", GetStatusComponent);
        _commands["/tool-approvals"] = new SlashCommand("/tool-approvals", "Change the tool approval mode for the session", null, true);

        // Session management commands
        _commands["/session clear"] = new SlashCommand("/session clear", "Clear the current session history", GetSessionClearComponent);
        _commands["/session list"] = new SlashCommand("/session list", "List and select from available sessions", null, true);
        _commands["/session rename"] = new SlashCommand("/session rename", "Rename the current session", GetSessionRenameComponent);
    }

    /// <summary>
    /// Gets an error message for unknown commands.
    /// </summary>
    private string GetUnknownCommandMessage(string command)
    {
        return $"Unknown command: {command}\nTip: Type /help to see available commands";
    }

    /// <summary>
    /// Gets help component for display.
    /// </summary>
    private ITuiComponent GetHelpComponent(string args)
    {
        return new HelpPanel(GetAllCommands());
    }

    /// <summary>
    /// Gets status component for display.
    /// </summary>
    private ITuiComponent GetStatusComponent(string args)
    {
        return new StatusPanel(_chatClient);
    }

    /// <summary>
    /// Gets clear component for display.
    /// </summary>
    private ITuiComponent GetClearComponent(string args)
    {
        ClearHistoryRequested?.Invoke();
        return new ClearPanel();
    }

    /// <summary>
    /// Gets exit component for display.
    /// </summary>
    private ITuiComponent GetExitComponent(string args)
    {
        ExitRequested?.Invoke();
        return new ExitPanel();
    }

    /// <summary>
    /// Gets session clear component for display.
    /// </summary>
    private ITuiComponent GetSessionClearComponent(string args)
    {
        SessionClearRequested?.Invoke();
        return new SessionClearPanel();
    }

    /// <summary>
    /// Gets session rename component for display.
    /// </summary>
    private ITuiComponent GetSessionRenameComponent(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            // For error cases, we could create an error panel, but for now return a simple message
            // This matches the existing behavior but with component styling
            return new SessionRenamePanel("Error: Session name cannot be empty");
        }

        SessionRenameRequested?.Invoke(args.Trim());
        return new SessionRenamePanel(args.Trim());
    }

}

/// <summary>
/// Represents a slash command with its name, description, and execution logic.
/// </summary>
/// <param name="Name">The command name (including the / prefix).</param>
/// <param name="Description">A description of what the command does.</param>
/// <param name="ExecuteWithComponent">The function to execute when the command is invoked, returning an ITuiComponent.</param>
/// <param name="IsInteractive">Whether the command requires interactive handling by the TUI.</param>
public sealed record SlashCommand(string Name, string Description, Func<string, ITuiComponent>? ExecuteWithComponent = null, bool IsInteractive = false);
