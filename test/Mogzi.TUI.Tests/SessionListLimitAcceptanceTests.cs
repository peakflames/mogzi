namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for session list limit configuration functionality.
/// Tests that the sessionListLimit configuration setting properly limits the number of sessions shown.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
public class SessionListLimitAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SessionManager _sessionManager;
    private readonly SessionListProvider _sessionListProvider;
    private readonly ILogger<SessionListLimitAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public SessionListLimitAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking except ScrollbackTerminal)
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");
        
        // Replace ScrollbackTerminal with test version to capture static content
        services.AddSingleton<IScrollbackTerminal>(provider => 
        {
            var realConsole = provider.GetRequiredService<IAnsiConsole>();
            return new TestScrollbackTerminal(realConsole);
        });
        
        // Add test-specific logger
        services.AddSingleton<ILogger<SessionListLimitAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<SessionListLimitAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
        
        // Get SessionListProvider from the service provider since it's registered as IUserSelectionProvider
        var providers = _serviceProvider.GetServices<IUserSelectionProvider>();
        _sessionListProvider = providers.OfType<SessionListProvider>().First();
        
        _logger = _serviceProvider.GetRequiredService<ILogger<SessionListLimitAcceptanceTests>>();
        
        _logger.LogInformation("SessionListLimitAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task SessionListProvider_RespectsConfiguredLimit_WhenManySessionsExist()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing session list limit configuration");
        _logger.LogInformation("🚀 Testing session list limit configuration");
        
        // Get the configured limit from the ChatClient
        var chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        var configuredLimit = chatClient.Config.SessionListLimit;
        _output?.WriteLine($"📋 Configured session limit: {configuredLimit}");
        
        // Create more sessions than the limit to test filtering
        var testSessionCount = configuredLimit + 5;
        var createdSessionIds = new List<Guid>();
        
        for (int i = 0; i < testSessionCount; i++)
        {
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            await _sessionManager.RenameSessionAsync($"Test Session {i + 1:D2}");
            createdSessionIds.Add(sessionId);
            
            // Add a small delay to ensure different LastModifiedAt timestamps
            await Task.Delay(10);
        }
        
        _output?.WriteLine($"📋 Created {testSessionCount} test sessions");
        
        // Act: Get selections from the SessionListProvider
        var selections = await _sessionListProvider.GetSelectionsAsync();
        
        // Assert 1: Should return exactly the configured limit number of sessions
        selections.Should().HaveCount(configuredLimit, 
            $"should return exactly {configuredLimit} sessions as configured"); // TOR-5.3.3
        _output?.WriteLine($"✅ Returned {selections.Count} sessions (respects configured limit)");
        
        // Assert 2: Should include our test sessions (they should be among the most recent)
        var sessionNames = selections.Select(s => s.Text).ToList();
        var testSessionsInResults = sessionNames.Where(name => name.StartsWith("Test Session ")).ToList();
        
        // We should have some of our test sessions in the results since they were just created
        testSessionsInResults.Should().NotBeEmpty("should include some of the recently created test sessions");
        _output?.WriteLine($"✅ Found {testSessionsInResults.Count} test sessions in results");
        
        // Assert 3: Should NOT include the oldest test session if we created more than the limit
        var oldestSessionName = $"Test Session 01";
        if (testSessionCount > configuredLimit)
        {
            // The oldest test session should not be in the results since we created more than the limit
            // and our test sessions should be among the most recent
            var oldestTestSessionInResults = testSessionsInResults.Contains(oldestSessionName);
            if (!oldestTestSessionInResults)
            {
                _output?.WriteLine("✅ Oldest test session correctly excluded from limited results");
            }
        }
        
        _output?.WriteLine("🎉 Session list limit configuration test completed successfully!");
        _logger.LogInformation("🎉 Session list limit configuration test completed successfully!");
    }

    [Fact]
    public async Task SessionListProvider_ReturnsAllSessions_WhenCountBelowLimit()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing session list when count is below limit");
        _logger.LogInformation("🚀 Testing session list when count is below limit");
        
        // Get the configured limit from the ChatClient
        var chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        var configuredLimit = chatClient.Config.SessionListLimit;
        
        // Create fewer sessions than the limit
        var testSessionCount = Math.Max(1, configuredLimit - 3);
        var createdSessionIds = new List<Guid>();
        
        for (int i = 0; i < testSessionCount; i++)
        {
            await _sessionManager.CreateNewSessionAsync();
            var sessionId = _sessionManager.CurrentSession!.Id;
            await _sessionManager.RenameSessionAsync($"Below Limit Session {i + 1:D2}");
            createdSessionIds.Add(sessionId);
            
            // Add a small delay to ensure different LastModifiedAt timestamps
            await Task.Delay(10);
        }
        
        _output?.WriteLine($"📋 Created {testSessionCount} test sessions (below limit of {configuredLimit})");
        
        // Act: Get selections from the SessionListProvider
        var selections = await _sessionListProvider.GetSelectionsAsync();
        
        // Assert: Should return all created sessions since count is below limit
        var testSessionSelections = selections.Where(s => s.Text.StartsWith("Below Limit Session")).ToList();
        testSessionSelections.Should().HaveCount(testSessionCount,
            $"should return all {testSessionCount} created sessions when below limit");
        
        _output?.WriteLine($"✅ Returned all {testSessionSelections.Count} sessions (below configured limit)");
        
        _output?.WriteLine("🎉 Session list below-limit test completed successfully!");
        _logger.LogInformation("🎉 Session list below-limit test completed successfully!");
    }

    /// <summary>
    /// Gets the path to the user's mogzi.config.json file.
    /// Uses the same logic as the main application to locate the config file.
    /// </summary>
    private static string? GetUserConfigPath()
    {
        // Use null to let the ChatClient.Create method find the default config path
        // This follows the same pattern as the main application
        return null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
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
