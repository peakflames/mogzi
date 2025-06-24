namespace MaxBot.TUI;

/// <summary>
/// Represents the status of a command execution
/// </summary>
public enum CommandStatus
{
    /// <summary>
    /// Command is currently running
    /// </summary>
    Running,
    
    /// <summary>
    /// Command completed successfully
    /// </summary>
    Success,
    
    /// <summary>
    /// Command failed with an error
    /// </summary>
    Error,
    
    /// <summary>
    /// Command was interrupted (e.g., Ctrl+C)
    /// </summary>
    Interrupted
}
