namespace MaxBot.TUI.Models;

/// <summary>
/// Represents the execution status of a tool.
/// </summary>
public enum ToolExecutionStatus
{
    /// <summary>Tool is currently executing</summary>
    Executing,

    /// <summary>Tool executed successfully</summary>
    Success,

    /// <summary>Tool execution failed</summary>
    Failed,

    /// <summary>Tool execution requires confirmation</summary>
    Confirming
}
