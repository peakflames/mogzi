namespace Mogzi.TUI.Tests;

[CollectionDefinition("Sequential_Session_Tests")]
public class MySequentialTestsCollection
{
    // This class definition is intentionally empty
}

/// <summary>
/// Base class for session-related acceptance tests that provides common session management functionality.
/// Implements proper test isolation by cleaning up test sessions between tests.
/// Follows the systems-first testing philosophy with real service configuration.
/// </summary>
public abstract class SessionTestBase : IDisposable
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly SessionManager _sessionManager;
    protected readonly ILogger _logger;
    protected readonly ITestOutputHelper? _output;
    protected bool _disposed = false;

    // Static semaphore to ensure only one test cleans up sessions at a time
    private static readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);

    protected SessionTestBase(ITestOutputHelper output, string loggerCategoryName)
    {
        _output = output;

        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();

        // Build service collection with real dependencies (no mocking except where specified)
        var services = new ServiceCollection();
        services.AddSingleton<IWorkingDirectoryProvider, TestWorkingDirectoryProvider>();


        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");

        // Add test-specific logger
        services.AddSingleton<ILogger>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(loggerCategoryName));

        _serviceProvider = services.BuildServiceProvider();

        // Get required services from DI container
        _sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
        _logger = _serviceProvider.GetRequiredService<ILogger>();

        _logger.LogInformation("{TestClass} initialized with real service configuration", loggerCategoryName);
    }

    /// <summary>
    /// Helper method to clear all test sessions to ensure clean test state.
    /// Only removes sessions that start with "Test" to avoid affecting user sessions.
    /// This method should be called at the beginning of each test to ensure isolation.
    /// Uses a semaphore to ensure thread-safe cleanup operations.
    /// </summary>
    protected async Task ClearAllTestSessionsAsync()
    {
        await _cleanupSemaphore.WaitAsync();
        try
        {
            var allSessions = await _sessionManager.ListSessionsAsync();
            var testSessions = allSessions.Where(s => s.Name.StartsWith("Test")).ToList();

            foreach (var session in testSessions)
            {
                try
                {
                    // Delete the session directory directly since SessionManager doesn't have a delete method
                    var sessionPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".mogzi", "chats", session.Id.ToString());

                    if (Directory.Exists(sessionPath))
                    {
                        Directory.Delete(sessionPath, true);
                        _output?.WriteLine($"🗑️ Cleaned up test session: {session.Name} ({session.Id})");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up test session {SessionId}", session.Id);
                }
            }

            if (testSessions.Count > 0)
            {
                _logger.LogInformation("Cleaned up {SessionCount} test sessions for test isolation", testSessions.Count);

                // Small delay to ensure file system operations complete
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear test sessions during setup");
        }
        finally
        {
            _cleanupSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the path to the user's mogzi.config.json file.
    /// Uses the same logic as the main application to locate the config file.
    /// </summary>
    protected static string? GetUserConfigPath()
    {
        // Use null to let the ChatClient.Create method find the default config path
        // This follows the same pattern as the main application
        return null;
    }

    public virtual void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            // Clean up any test sessions created during tests - must be synchronous to ensure cleanup completes
            ClearAllTestSessionsAsync().GetAwaiter().GetResult();

            if (_serviceProvider is IDisposable disposableProvider)
            {
                disposableProvider.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during test cleanup");
        }

        GC.SuppressFinalize(this);
    }
}
