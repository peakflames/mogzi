namespace Mogzi.TUI.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a maximum length, adding ellipsis if needed
    /// </summary>
    public static string Truncate(this string value, int maxLength, string ellipsis = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= ellipsis.Length)
        {
            return ellipsis[..maxLength];
        }

        return value[..(maxLength - ellipsis.Length)] + ellipsis;
    }

    /// <summary>
    /// Pads a string to a specific width, truncating if necessary
    /// </summary>
    public static string PadAndTruncate(this string value, int width, char paddingChar = ' ')
    {
        if (string.IsNullOrEmpty(value))
        {
            return new string(paddingChar, width);
        }

        if (value.Length > width)
        {
            return value.Truncate(width);
        }

        return value.PadRight(width, paddingChar);
    }

    /// <summary>
    /// Centers text within a given width
    /// </summary>
    public static string Center(this string value, int width, char paddingChar = ' ')
    {
        if (string.IsNullOrEmpty(value) || value.Length >= width)
        {
            return value.PadAndTruncate(width, paddingChar);
        }

        var totalPadding = width - value.Length;
        var leftPadding = totalPadding / 2;
        var rightPadding = totalPadding - leftPadding;

        return new string(paddingChar, leftPadding) + value + new string(paddingChar, rightPadding);
    }

    /// <summary>
    /// Removes ANSI escape sequences from a string
    /// </summary>
    public static string RemoveAnsiCodes(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Simple regex to remove ANSI escape sequences
        return System.Text.RegularExpressions.Regex.Replace(value, @"\x1B\[[0-?]*[ -/]*[@-~]", "");
    }

    /// <summary>
    /// Gets the display width of a string (excluding ANSI codes)
    /// </summary>
    public static int GetDisplayWidth(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return value.RemoveAnsiCodes().Length;
    }

    /// <summary>
    /// Wraps text to fit within a specified width
    /// </summary>
    public static IEnumerable<string> WordWrap(this string value, int width)
    {
        if (string.IsNullOrEmpty(value) || width <= 0)
        {
            yield return value ?? string.Empty;
            yield break;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length == 0)
            {
                _ = currentLine.Append(word);
            }
            else if (currentLine.Length + 1 + word.Length <= width)
            {
                _ = currentLine.Append(' ').Append(word);
            }
            else
            {
                yield return currentLine.ToString();
                _ = currentLine.Clear().Append(word);
            }
        }

        if (currentLine.Length > 0)
        {
            yield return currentLine.ToString();
        }
    }

    /// <summary>
    /// Escapes special characters for safe display
    /// </summary>
    public static string EscapeForDisplay(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\t", "\\t")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\0", "\\0");
    }

    /// <summary>
    /// Checks if a string contains only whitespace or control characters
    /// </summary>
    public static bool IsWhitespaceOrControl(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return value.All(c => char.IsWhiteSpace(c) || char.IsControl(c));
    }

    /// <summary>
    /// Safely gets a substring without throwing exceptions
    /// </summary>
    public static string SafeSubstring(this string value, int startIndex, int length = -1)
    {
        if (string.IsNullOrEmpty(value) || startIndex < 0 || startIndex >= value.Length)
        {
            return string.Empty;
        }

        if (length < 0)
        {
            return value[startIndex..];
        }

        var maxLength = Math.Min(length, value.Length - startIndex);
        return value.Substring(startIndex, maxLength);
    }

    /// <summary>
    /// Repeats a string a specified number of times
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        if (string.IsNullOrEmpty(value) || count <= 0)
        {
            return string.Empty;
        }

        if (count == 1)
        {
            return value;
        }

        var result = new StringBuilder(value.Length * count);
        for (var i = 0; i < count; i++)
        {
            _ = result.Append(value);
        }
        return result.ToString();
    }

    /// <summary>
    /// Converts a string to a safe filename
    /// </summary>
    public static string ToSafeFileName(this string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "unnamed";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var result = new StringBuilder();

        foreach (var c in value)
        {
            _ = invalidChars.Contains(c) ? result.Append('_') : result.Append(c);
        }

        return result.ToString().Trim();
    }
}
