namespace Mogzi.TUI.Commands;

/// <summary>
/// Interface for command implementations.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the command name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the command description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command with the given arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    Task<int> ExecuteAsync(string[] args);

    /// <summary>
    /// Shows help for this command.
    /// </summary>
    void ShowHelp();
}
