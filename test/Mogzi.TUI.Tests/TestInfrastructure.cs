namespace Mogzi.TUI.Tests;

/// <summary>
/// Test implementation of IScrollbackTerminal that captures static content for verification.
/// </summary>
public class TestScrollbackTerminal : IScrollbackTerminal, IDisposable
{
    private readonly List<StaticContentEntry> _staticContent = new();
    private readonly IAnsiConsole _realConsole;
    private readonly Lock _lock = new();
    private bool _disposed = false;

    public TestScrollbackTerminal(IAnsiConsole realConsole)
    {
        _realConsole = realConsole;
    }

    public IReadOnlyList<StaticContentEntry> StaticContent 
    {
        get
        {
            lock (_lock)
            {
                return _staticContent.ToList().AsReadOnly();
            }
        }
    }

    public void Initialize()
    {
        // Delegate to real console for actual initialization
        _realConsole.Clear();
        _realConsole.Cursor.SetPosition(0, 0);
        _realConsole.Cursor.Hide();
    }

    public void WriteStatic(IRenderable content, bool isUpdatable = false)
    {
        if (_disposed)
            return;

        lock (_lock)
        {
            var textContent = ExtractTextFromRenderable(content);
            
            // Simulate real ScrollbackTerminal behavior for updatable content
            if (isUpdatable)
            {
                // Remove all previous updatable entries (simulates ClearUpdatableContent)
                // This matches the real terminal's behavior where updatable content replaces previous updatable content
                for (int i = _staticContent.Count - 1; i >= 0; i--)
                {
                    if (_staticContent[i].IsUpdatable)
                    {
                        _staticContent.RemoveAt(i);
                    }
                }
            }
            else
            {
                // When writing non-updatable content, clear all previous updatable content
                // This simulates the real terminal's ClearUpdatableContent() call for non-updatable writes
                for (int i = _staticContent.Count - 1; i >= 0; i--)
                {
                    if (_staticContent[i].IsUpdatable)
                    {
                        _staticContent.RemoveAt(i);
                    }
                }
            }
            
            var entry = new StaticContentEntry
            {
                Content = textContent,
                Timestamp = DateTime.UtcNow,
                IsUpdatable = isUpdatable
            };
            
            _staticContent.Add(entry);
        }
        
        // Optionally write to real console for debugging
        // _realConsole?.Write(content);
    }

    public async Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken)
    {
        // For testing, we don't need the dynamic display loop
        // Just complete immediately
        await Task.CompletedTask;
    }

    public void Shutdown()
    {
        _realConsole.Cursor.Show();
    }

    public void Refresh()
    {
        // No-op for testing
    }

    /// <summary>
    /// Extracts plain text from a Spectre.Console IRenderable for assertion purposes.
    /// </summary>
    private static string ExtractTextFromRenderable(IRenderable renderable)
    {
        var stringWriter = new StringWriter();
        var testOutput = new AnsiConsoleOutput(stringWriter);
        var testConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = testOutput
        });
        
        testConsole.Write(renderable);
        var rawOutput = stringWriter.ToString();
        
        // Remove ANSI escape sequences and normalize whitespace
        var cleanText = Regex.Replace(rawOutput, @"\x1B\[[0-9;]*[mK]", "");
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();
        
        return cleanText;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
            
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a static content entry captured by TestScrollbackTerminal.
/// </summary>
public record StaticContentEntry
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public bool IsUpdatable { get; init; }
}
