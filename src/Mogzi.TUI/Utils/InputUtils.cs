namespace Mogzi.TUI.Utils;

/// <summary>
/// Utility methods for input handling and text manipulation.
/// </summary>
public static class InputUtils
{
    /// <summary>
    /// Determines if autocomplete should be shown for slash commands.
    /// For slash commands, we check if there's a '/' at the beginning of the current line
    /// and the cursor is positioned within a potential slash command.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPos">The current cursor position.</param>
    /// <returns>True if slash autocomplete should be shown.</returns>
    public static bool ShouldShowSlashAutocomplete(string input, int cursorPos)
    {
        if (string.IsNullOrEmpty(input) || cursorPos < 0 || cursorPos > input.Length)
        {
            return false;
        }

        // For slash commands, check if the input starts with '/' and cursor is within the command
        if (input.Length > 0 && input[0] == '/')
        {
            // Check if cursor is positioned within the slash command (no newlines between '/' and cursor)
            for (var i = 0; i < Math.Min(cursorPos, input.Length); i++)
            {
                if (input[i] is '\n' or '\r')
                {
                    return false; // Found newline before cursor, not in slash command
                }
            }
            return true; // Cursor is within a slash command
        }

        return false;
    }

    /// <summary>
    /// Extracts the slash command at the cursor position.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPos">The current cursor position.</param>
    /// <returns>The partial slash command, or empty string if none found.</returns>
    public static string ExtractSlashCommand(string input, int cursorPos)
    {
        var (startPos, length) = FindSlashCommandBounds(input, cursorPos);
        return startPos >= 0 ? input.Substring(startPos, length) : string.Empty;
    }

    /// <summary>
    /// Finds the bounds of the slash command at the cursor position.
    /// For slash commands, we extract from the '/' to the end of the input or until we hit a non-command character.
    /// This allows multi-word commands like "/session list" to be treated as a single command.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPos">The current cursor position.</param>
    /// <returns>A tuple containing the start position and length of the command.</returns>
    public static (int startPos, int length) FindSlashCommandBounds(string input, int cursorPos)
    {
        if (string.IsNullOrEmpty(input) || cursorPos < 0 || cursorPos > input.Length)
        {
            return (-1, 0);
        }

        // Find the start of the slash command by looking backwards for '/'
        var startPos = -1;
        for (var i = Math.Min(cursorPos, input.Length - 1); i >= 0; i--)
        {
            if (input[i] == '/')
            {
                startPos = i;
                break;
            }
            // If we hit a newline or other non-command character, stop looking
            if (input[i] is '\n' or '\r')
            {
                break;
            }
        }

        if (startPos < 0)
        {
            return (-1, 0);
        }

        // For slash commands, extract from '/' to the cursor position
        // This allows partial matching of multi-word commands
        var length = cursorPos - startPos;
        return (startPos, length);
    }

    /// <summary>
    /// Replaces the slash command at the cursor position with a new command.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPos">The current cursor position.</param>
    /// <param name="newCommand">The new command to insert.</param>
    /// <returns>A tuple containing the new input text and cursor position.</returns>
    public static (string newInput, int newCursorPos) ReplaceSlashCommand(string input, int cursorPos, string newCommand)
    {
        var (startPos, length) = FindSlashCommandBounds(input, cursorPos);
        if (startPos < 0)
        {
            return (input, cursorPos);
        }

        var newInput = input.Remove(startPos, length).Insert(startPos, newCommand + " ");
        var newCursorPos = startPos + newCommand.Length + 1;

        return (newInput, newCursorPos);
    }


}
