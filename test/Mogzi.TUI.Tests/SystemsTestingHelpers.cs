namespace Mogzi.TUI.Tests;

/// <summary>
/// Result of executing the application process for systems-level testing.
/// </summary>
public record ProcessResult(int ExitCode, string Output, string Error);

/// <summary>
/// Helper methods for systems-level testing that executes the complete application.
/// Supports the systems-first testing philosophy by testing user workflows end-to-end.
/// </summary>
public static class SystemsTestingHelpers
{
    private static readonly string ProjectPath = GetProjectPath();

    /// <summary>
    /// Dynamically finds the Mogzi.TUI project path by searching up the directory tree.
    /// This ensures cross-platform compatibility and works regardless of repository location.
    /// </summary>
    private static string GetProjectPath()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        // Search up the directory tree for the src/Mogzi.TUI directory
        var searchDir = currentDir;
        while (searchDir != null)
        {
            var mogziTuiPath = Path.Combine(searchDir, "src", "Mogzi.TUI");
            if (Directory.Exists(mogziTuiPath))
            {
                var projectFile = Path.Combine(mogziTuiPath, "Mogzi.TUI.csproj");
                if (File.Exists(projectFile))
                {
                    return mogziTuiPath;
                }
            }
            
            var parentDir = Directory.GetParent(searchDir);
            searchDir = parentDir?.FullName;
        }
        
        throw new InvalidOperationException(
            $"Could not find Mogzi.TUI project directory. Started search from: {currentDir}");
    }

    /// <summary>
    /// Executes the Mogzi application with specified arguments and optional input.
    /// This enables testing complete user workflows through the actual application.
    /// </summary>
    /// <param name="args">Command line arguments to pass to the application</param>
    /// <param name="input">Optional input to send to the application's stdin</param>
    /// <param name="timeout">Maximum time to wait for the application to complete</param>
    /// <returns>Process result containing exit code, output, and error streams</returns>
    public static async Task<ProcessResult> ExecuteApplicationAsync(
        string[] args, 
        string? input = null, 
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{ProjectPath}\" -- {string.Join(" ", args)}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = ProjectPath
            }
        };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        if (input != null)
        {
            await process.StandardInput.WriteLineAsync(input);
            await process.StandardInput.FlushAsync();
            process.StandardInput.Close();
        }
        
        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            
            // Capture output even when killed due to timeout
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            
            // Return a special result indicating timeout but with captured output
            return new ProcessResult(-1, output, error);
        }
        
        var finalOutput = outputBuilder.ToString();
        var finalError = errorBuilder.ToString();
        
        return new ProcessResult(process.ExitCode, finalOutput, finalError);
    }

    /// <summary>
    /// Executes the application with piped input to test pipe functionality.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="pipedInput">Content to pipe to the application</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <returns>Process result</returns>
    public static async Task<ProcessResult> ExecuteApplicationWithPipeAsync(
        string[] args,
        string pipedInput,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromSeconds(30);
        
        // Cross-platform approach: Use ProcessStartInfo to pipe input directly
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{ProjectPath}\" -- {string.Join(" ", args)}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = ProjectPath
            }
        };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // Write the piped input to stdin and close it to simulate piping
        if (!string.IsNullOrEmpty(pipedInput))
        {
            await process.StandardInput.WriteAsync(pipedInput);
            await process.StandardInput.FlushAsync();
        }
        process.StandardInput.Close();
        
        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            
            // Capture output even when killed due to timeout
            var output = outputBuilder.ToString();
            var error = errorBuilder.ToString();
            
            // Return a special result indicating timeout but with captured output
            return new ProcessResult(-1, output, error);
        }
        
        var finalOutput = outputBuilder.ToString();
        var finalError = errorBuilder.ToString();
        
        return new ProcessResult(process.ExitCode, finalOutput, finalError);
    }

    /// <summary>
    /// Extracts session ID from application output for systems-level testing.
    /// Looks for session ID patterns in user-visible output.
    /// </summary>
    /// <param name="output">Application output to search</param>
    /// <returns>Session ID if found</returns>
    /// <exception cref="InvalidOperationException">If session ID cannot be found</exception>
    public static string ExtractSessionIdFromOutput(string output)
    {
        // Look for various session ID patterns in output
        var patterns = new[]
        {
            @"Session:\s*([a-f0-9-]{36})",           // "Session: uuid"
            @"session\s+([a-f0-9-]{36})",           // "session uuid"
            @"Loading.*?([a-f0-9-]{36})",           // "Loading ... uuid"
            @"([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})" // Any UUID pattern
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(output, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        
        throw new InvalidOperationException($"Session ID not found in output: {output}");
    }

    /// <summary>
    /// Creates a temporary test file for attachment testing.
    /// </summary>
    /// <param name="filename">Name of the file to create</param>
    /// <param name="content">Content to write to the file</param>
    /// <returns>Full path to the created file</returns>
    public static string CreateTestFile(string filename, string content)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mogzi-test-files", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        var filePath = Path.Combine(tempDir, filename);
        File.WriteAllText(filePath, content);
        
        return filePath;
    }

    /// <summary>
    /// Creates a temporary test file with binary content for attachment testing.
    /// </summary>
    /// <param name="filename">Name of the file to create</param>
    /// <param name="content">Binary content to write to the file</param>
    /// <returns>Full path to the created file</returns>
    public static string CreateTestFile(string filename, byte[] content)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "mogzi-test-files", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        
        var filePath = Path.Combine(tempDir, filename);
        File.WriteAllBytes(filePath, content);
        
        return filePath;
    }

    /// <summary>
    /// Gets the session file path for a given session ID.
    /// Used for optional file system validation in systems tests.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Path to session.json file</returns>
    public static string GetSessionFilePath(string sessionId)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDirectory, ".mogzi", "chats", sessionId, "session.json");
    }

    /// <summary>
    /// Corrupts a session file to test error handling scenarios.
    /// Used for testing graceful degradation in systems tests.
    /// </summary>
    /// <param name="sessionId">Session ID to corrupt</param>
    public static void CorruptSessionFile(string sessionId)
    {
        var sessionFile = GetSessionFilePath(sessionId);
        if (File.Exists(sessionFile))
        {
            File.WriteAllText(sessionFile, "{ invalid json content");
        }
    }

    /// <summary>
    /// Cleans up test files and directories created during testing.
    /// </summary>
    /// <param name="sessionId">Optional session ID to clean up specific session</param>
    public static void CleanupTestFiles(string? sessionId = null)
    {
        try
        {
            // Clean up temporary test files
            var tempTestDir = Path.Combine(Path.GetTempPath(), "mogzi-test-files");
            if (Directory.Exists(tempTestDir))
            {
                Directory.Delete(tempTestDir, true);
            }
            
            // Clean up specific session if provided
            if (!string.IsNullOrEmpty(sessionId))
            {
                var sessionDir = Path.GetDirectoryName(GetSessionFilePath(sessionId));
                if (sessionDir != null && Directory.Exists(sessionDir))
                {
                    Directory.Delete(sessionDir, true);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}

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
