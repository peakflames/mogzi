namespace Mogzi.TUI.Utils;

/// <summary>
/// Utility methods for input handling and text manipulation.
/// </summary>
public static class InputUtils
{
    /// <summary>
    /// Determines if autocomplete should be shown for slash commands.
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

        // Find the word at cursor position
        var wordStart = FindWordStart(input, cursorPos);
        return wordStart >= 0 && wordStart < input.Length && input[wordStart] == '/';
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
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPos">The current cursor position.</param>
    /// <returns>A tuple containing the start position and length of the command.</returns>
    public static (int startPos, int length) FindSlashCommandBounds(string input, int cursorPos)
    {
        var wordStart = FindWordStart(input, cursorPos);
        if (wordStart < 0 || wordStart >= input.Length || input[wordStart] != '/')
        {
            return (-1, 0);
        }

        var wordEnd = FindWordEnd(input, cursorPos);
        return (wordStart, wordEnd - wordStart);
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

    /// <summary>
    /// Finds the start of the word at the given position.
    /// </summary>
    /// <param name="input">The input text.</param>
    /// <param name="pos">The position to search from.</param>
    /// <returns>The start position of the word, or -1 if not found.</returns>
    private static int FindWordStart(string input, int pos)
    {
        if (string.IsNullOrEmpty(input) || pos < 0)
        {
            return -1;
        }

        // Adjust position to be within bounds
        pos = Math.Min(pos, input.Length - 1);

        // If we're at a space, move back to find the previous word
        while (pos >= 0 && char.IsWhiteSpace(input[pos]))
        {
            pos--;
        }

        if (pos < 0)
        {
            return -1;
        }

        // Find the start of the current word
        while (pos > 0 && !char.IsWhiteSpace(input[pos - 1]))
        {
            pos--;
        }

        return pos;
    }

    /// <summary>
    /// Finds the end of the word at the given position.
    /// </summary>
    /// <param name="input">The input text.</param>
    /// <param name="pos">The position to search from.</param>
    /// <returns>The end position of the word.</returns>
    private static int FindWordEnd(string input, int pos)
    {
        if (string.IsNullOrEmpty(input) || pos < 0)
        {
            return 0;
        }

        // Adjust position to be within bounds
        pos = Math.Min(pos, input.Length);

        // If we're at the end or at a space, find the end of the previous word
        if (pos >= input.Length || char.IsWhiteSpace(input[pos]))
        {
            while (pos > 0 && char.IsWhiteSpace(input[pos - 1]))
            {
                pos--;
            }
        }
        else
        {
            // Find the end of the current word
            while (pos < input.Length && !char.IsWhiteSpace(input[pos]))
            {
                pos++;
            }
        }

        return pos;
    }
}
