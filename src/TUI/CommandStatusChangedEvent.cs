namespace MaxBot.TUI;

/// <summary>
/// Event published when a command's status changes during execution
/// </summary>
public class CommandStatusChangedEvent : ITuiEvent
{
    /// <summary>
    /// The command that was executed
    /// </summary>
    public string Command { get; }
    
    /// <summary>
    /// The current status of the command
    /// </summary>
    public CommandStatus Status { get; }
    
    /// <summary>
    /// The output from the command (stdout/stderr)
    /// </summary>
    public string Output { get; }
    
    /// <summary>
    /// Unique identifier for this command execution
    /// </summary>
    public string CommandId { get; }
    
    /// <summary>
    /// Timestamp when the status changed
    /// </summary>
    public DateTime Timestamp { get; }

    public CommandStatusChangedEvent(string commandId, string command, CommandStatus status, string output)
    {
        CommandId = commandId;
        Command = command;
        Status = status;
        Output = output;
        Timestamp = DateTime.UtcNow;
    }
}
