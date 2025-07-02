
namespace Mogzi.TUI.Infrastructure;

/// <summary>
/// AOT-compatible file logger provider that writes logs to ~/.mogzi/logs with rolling files.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly LogLevel _minLogLevel;
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private bool _disposed = false;

    public FileLoggerProvider(LogLevel minLogLevel)
    {
        _minLogLevel = minLogLevel;

        // Create log directory at ~/.mogzi/logs
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _logDirectory = Path.Combine(homeDirectory, ".mogzi", "logs");

        try
        {
            _ = Directory.CreateDirectory(_logDirectory);
        }
        catch (Exception ex)
        {
            // Fallback to temp directory if we can't create ~/.mogzi/logs
            _logDirectory = Path.Combine(Path.GetTempPath(), "mogzi-logs");
            _ = Directory.CreateDirectory(_logDirectory);
            Console.WriteLine($"Warning: Could not create ~/.mogzi/logs, using {_logDirectory}. Error: {ex.Message}");
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory, _minLogLevel));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var logger in _loggers.Values)
        {
            logger.Dispose();
        }

        _loggers.Clear();
    }
}

/// <summary>
/// AOT-compatible file logger that writes to rolling log files.
/// </summary>
public sealed class FileLogger(string categoryName, string logDirectory, LogLevel minLogLevel) : ILogger, IDisposable
{
    private readonly string _categoryName = categoryName;
    private readonly string _logDirectory = logDirectory;
    private readonly LogLevel _minLogLevel = minLogLevel;
    private readonly Lock _lock = new();
    private StreamWriter? _currentWriter;
    private string? _currentLogFile;
    private DateTime _currentLogDate = DateTime.Today;
    private bool _disposed = false;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null; // Simple implementation - no scope support
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLogLevel && _minLogLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || _disposed)
        {
            return;
        }

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null)
        {
            return;
        }

        lock (_lock)
        {
            try
            {
                EnsureLogFile();

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLevelString = GetLogLevelString(logLevel);
                var logEntry = $"[{timestamp}] [{logLevelString}] {_categoryName}: {message}";

                if (exception != null)
                {
                    logEntry += Environment.NewLine + exception.ToString();
                }

                _currentWriter?.WriteLine(logEntry);
                _currentWriter?.Flush();
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }
    }

    private void EnsureLogFile()
    {
        var today = DateTime.Today;

        // Check if we need to roll to a new log file
        if (_currentWriter == null || _currentLogDate != today)
        {
            _currentWriter?.Dispose();

            _currentLogDate = today;
            var logFileName = $"mogzi-{today:yyyy-MM-dd}.log";
            _currentLogFile = Path.Combine(_logDirectory, logFileName);

            _currentWriter = new StreamWriter(_currentLogFile, append: true)
            {
                AutoFlush = true
            };

            // Clean up old log files (keep last 30 days)
            CleanupOldLogFiles();
        }
    }

    private void CleanupOldLogFiles()
    {
        try
        {
            var cutoffDate = DateTime.Today.AddDays(-30);
            var logFiles = Directory.GetFiles(_logDirectory, "mogzi-*.log");

            foreach (var logFile in logFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(logFile);
                if (fileName.StartsWith("mogzi-") && fileName.Length >= 17) // "mogzi-yyyy-MM-dd"
                {
                    var dateString = fileName[7..]; // Remove "mogzi-" prefix
                    if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var fileDate))
                    {
                        if (fileDate < cutoffDate)
                        {
                            File.Delete(logFile);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            _ => "UNKN"
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        lock (_lock)
        {
            _currentWriter?.Dispose();
            _currentWriter = null;
        }
    }
}
