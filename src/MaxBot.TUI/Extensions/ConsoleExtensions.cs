namespace MaxBot.TUI.Extensions;

public static class ConsoleExtensions
{
    /// <summary>
    /// Gets the current terminal dimensions safely
    /// </summary>
    public static (int Width, int Height) GetTerminalSize()
    {
        try
        {
            return (Console.WindowWidth, Console.WindowHeight);
        }
        catch
        {
            // Fallback to reasonable defaults if terminal size cannot be determined
            return (80, 24);
        }
    }

    /// <summary>
    /// Checks if the console supports ANSI escape sequences
    /// </summary>
    public static bool SupportsAnsi()
    {
        try
        {
            // Check if we're in a terminal that supports ANSI
            return !Console.IsOutputRedirected && 
                   Environment.GetEnvironmentVariable("TERM") != null ||
                   Environment.GetEnvironmentVariable("COLORTERM") != null ||
                   Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely sets the console cursor position
    /// </summary>
    public static bool TrySetCursorPosition(int left, int top)
    {
        try
        {
            var (width, height) = GetTerminalSize();
            if (left >= 0 && left < width && top >= 0 && top < height)
            {
                Console.SetCursorPosition(left, top);
                return true;
            }
        }
        catch
        {
            // Ignore cursor positioning errors
        }
        return false;
    }

    /// <summary>
    /// Safely gets the current cursor position
    /// </summary>
    public static (int Left, int Top) GetCursorPosition()
    {
        try
        {
            return (Console.CursorLeft, Console.CursorTop);
        }
        catch
        {
            return (0, 0);
        }
    }

    /// <summary>
    /// Clears the current line
    /// </summary>
    public static void ClearCurrentLine()
    {
        try
        {
            var (left, top) = GetCursorPosition();
            var (width, _) = GetTerminalSize();
            
            Console.SetCursorPosition(0, top);
            Console.Write(new string(' ', width));
            Console.SetCursorPosition(left, top);
        }
        catch
        {
            // Ignore errors
        }
    }

    /// <summary>
    /// Writes text at a specific position without changing the current cursor position
    /// </summary>
    public static void WriteAt(int left, int top, string text)
    {
        var originalPosition = GetCursorPosition();
        
        if (TrySetCursorPosition(left, top))
        {
            Console.Write(text);
            TrySetCursorPosition(originalPosition.Left, originalPosition.Top);
        }
    }

    /// <summary>
    /// Checks if the terminal is in a valid state for TUI operations
    /// </summary>
    public static bool IsTerminalReady()
    {
        try
        {
            var (width, height) = GetTerminalSize();
            return width > 10 && height > 5 && !Console.IsInputRedirected && !Console.IsOutputRedirected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Safely hides the cursor
    /// </summary>
    public static void HideCursor()
    {
        try
        {
            Console.CursorVisible = false;
        }
        catch
        {
            // Ignore cursor visibility errors
        }
    }

    /// <summary>
    /// Safely shows the cursor
    /// </summary>
    public static void ShowCursor()
    {
        try
        {
            Console.CursorVisible = true;
        }
        catch
        {
            // Ignore cursor visibility errors
        }
    }
}
