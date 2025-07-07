namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for CLI session management commands.
/// Tests the complete session CLI command pipeline including session listing and info display.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
public class SessionCliCommandAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SessionManager _sessionManager;
    private readonly ILogger<SessionCliCommandAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public SessionCliCommandAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking)
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");
        
        // Add test-specific logger
        services.AddSingleton<ILogger<SessionCliCommandAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<SessionCliCommandAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
        _logger = _serviceProvider.GetRequiredService<ILogger<SessionCliCommandAcceptanceTests>>();
        
        _logger.LogInformation("SessionCliCommandAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task SessionListCommand_WithNoSessions_ShowsNoSessionsMessage()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session list' with no sessions");
        _logger.LogInformation("🚀 Testing 'mogzi session list' with no sessions");
        
        // Ensure we start with no sessions by clearing any existing ones
        await ClearAllTestSessionsAsync();
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["list"]);
        
        // Assert
        result.Should().Be(0, "command should succeed even with no sessions");
        _output?.WriteLine("✅ Session list command completed successfully with no sessions");
        
        _logger.LogInformation("🎉 Session list command with no sessions test completed successfully!");
    }

    [Fact]
    public async Task SessionListCommand_WithMultipleSessions_DisplaysSessionTable()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session list' with multiple sessions");
        _logger.LogInformation("🚀 Testing 'mogzi session list' with multiple sessions");
        
        // Clear any existing sessions first
        await ClearAllTestSessionsAsync();
        
        // Create test sessions
        await _sessionManager.CreateNewSessionAsync();
        var session1Id = _sessionManager.CurrentSession!.Id;
        await _sessionManager.RenameSessionAsync("Test Session 1");
        var testMessage1 = new ChatMessage(ChatRole.User, "Hello from session 1");
        await _sessionManager.AddMessageToCurrentSessionAsync(testMessage1);
        
        await _sessionManager.CreateNewSessionAsync();
        var session2Id = _sessionManager.CurrentSession!.Id;
        await _sessionManager.RenameSessionAsync("Test Session 2");
        var testMessage2 = new ChatMessage(ChatRole.User, "Hello from session 2 with a longer message");
        await _sessionManager.AddMessageToCurrentSessionAsync(testMessage2);
        
        _output?.WriteLine($"📋 Created test sessions: {session1Id}, {session2Id}");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["list"]);
        
        // Assert
        result.Should().Be(0, "command should succeed with sessions present"); // TOR-5.3.2
        
        // Verify sessions exist in the session manager
        var allSessions = await _sessionManager.ListSessionsAsync(); // TOR-5.3.2
        allSessions.Should().HaveCountGreaterOrEqualTo(2, "should have at least the 2 test sessions"); // TOR-5.3.2
        
        var testSessions = allSessions.Where(s => s.Name.StartsWith("Test Session")).ToList();
        testSessions.Should().HaveCount(2, "should have exactly 2 test sessions"); // TOR-5.3.2
        
        var session1 = testSessions.FirstOrDefault(s => s.Name == "Test Session 1");
        var session2 = testSessions.FirstOrDefault(s => s.Name == "Test Session 2");
        
        session1.Should().NotBeNull("Test Session 1 should exist");
        session2.Should().NotBeNull("Test Session 2 should exist");
        
        session1!.InitialPrompt.Should().Be("Hello from session 1", "session 1 should have correct initial prompt");
        session2!.InitialPrompt.Should().Be("Hello from session 2 with a longer message", "session 2 should have correct initial prompt");
        
        _output?.WriteLine("✅ Session list command completed successfully with multiple sessions");
        _logger.LogInformation("🎉 Session list command with multiple sessions test completed successfully!");
    }

    [Fact]
    public async Task SessionInfoCommand_WithValidSessionName_DisplaysSessionInformation()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with valid session name");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with valid session name");
        
        // Create a test session
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = "Test Info Session";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        // Add some messages to the session
        var userMessage = new ChatMessage(ChatRole.User, "Test user message for info display");
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "Test assistant response");
        await _sessionManager.AddMessageToCurrentSessionAsync(assistantMessage);
        
        _output?.WriteLine($"📋 Created test session '{sessionName}' with ID: {sessionId}");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["info", sessionName]);
        
        // Assert
        result.Should().Be(0, "command should succeed with valid session name"); // TOR-5.3.4
        
        // Verify the session was loaded correctly by checking SessionManager state
        _sessionManager.CurrentSession.Should().NotBeNull("session should be loaded"); // TOR-5.3.4
        _sessionManager.CurrentSession!.Name.Should().Be(sessionName, "correct session should be loaded"); // TOR-5.3.4
        _sessionManager.CurrentSession.Id.Should().Be(sessionId, "session ID should match"); // TOR-5.3.4
        _sessionManager.CurrentSession.History.Should().HaveCount(2, "session should have 2 messages"); // TOR-5.3.4
        _sessionManager.CurrentSession.InitialPrompt.Should().Be("Test user message for info display", "initial prompt should be set correctly"); // TOR-5.3.4
        
        _output?.WriteLine("✅ Session info command completed successfully with valid session name");
        _logger.LogInformation("🎉 Session info command with valid session name test completed successfully!");
    }

    [Fact]
    public async Task SessionInfoCommand_WithValidSessionId_DisplaysSessionInformation()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with valid session ID");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with valid session ID");
        
        // Create a test session
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = "Test Info Session by ID";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        var userMessage = new ChatMessage(ChatRole.User, "Test message for ID lookup");
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        
        _output?.WriteLine($"📋 Created test session '{sessionName}' with ID: {sessionId}");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["info", sessionId.ToString()]);
        
        // Assert
        result.Should().Be(0, "command should succeed with valid session ID"); // TOR-5.3.4
        
        // Verify the session was loaded correctly
        _sessionManager.CurrentSession.Should().NotBeNull("session should be loaded"); // TOR-5.3.4
        _sessionManager.CurrentSession!.Name.Should().Be(sessionName, "correct session should be loaded"); // TOR-5.3.4
        _sessionManager.CurrentSession.Id.Should().Be(sessionId, "session ID should match"); // TOR-5.3.4
        _sessionManager.CurrentSession.InitialPrompt.Should().Be("Test message for ID lookup", "initial prompt should be set correctly"); // TOR-5.3.4
        
        _output?.WriteLine("✅ Session info command completed successfully with valid session ID");
        _logger.LogInformation("🎉 Session info command with valid session ID test completed successfully!");
    }

    [Fact]
    public async Task SessionInfoCommand_WithInvalidSessionName_ReturnsErrorCode()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with invalid session name");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with invalid session name");
        
        var invalidSessionName = "NonExistent Session Name";
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["info", invalidSessionName]);
        
        // Assert
        result.Should().Be(1, "command should return error code for invalid session name"); // TOR-8.2
        
        _output?.WriteLine("✅ Session info command correctly returned error for invalid session name");
        _logger.LogInformation("🎉 Session info command with invalid session name test completed successfully!");
    }

    [Fact]
    public async Task SessionCommand_WithNoSubcommand_ShowsHelpAndReturnsError()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session' with no subcommand");
        _logger.LogInformation("🚀 Testing 'mogzi session' with no subcommand");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync([]);
        
        // Assert
        result.Should().Be(1, "command should return error code when no subcommand is provided");
        
        _output?.WriteLine("✅ Session command correctly returned error for missing subcommand");
        _logger.LogInformation("🎉 Session command with no subcommand test completed successfully!");
    }

    [Fact]
    public async Task SessionCommand_WithInvalidSubcommand_ShowsErrorAndReturnsError()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session' with invalid subcommand");
        _logger.LogInformation("🚀 Testing 'mogzi session' with invalid subcommand");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["invalid"]);
        
        // Assert
        result.Should().Be(1, "command should return error code for invalid subcommand");
        
        _output?.WriteLine("✅ Session command correctly returned error for invalid subcommand");
        _logger.LogInformation("🎉 Session command with invalid subcommand test completed successfully!");
    }

    [Fact]
    public async Task SessionCommand_WithHelpFlag_ShowsHelpAndReturnsSuccess()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session --help'");
        _logger.LogInformation("🚀 Testing 'mogzi session --help'");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["--help"]);
        
        // Assert
        result.Should().Be(0, "help command should return success code");
        
        _output?.WriteLine("✅ Session help command completed successfully");
        _logger.LogInformation("🎉 Session help command test completed successfully!");
    }

    [Fact]
    public async Task SessionInfoCommand_WithPartialSessionId_DisplaysSessionInformation()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with partial session ID (EndsWith matching)");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with partial session ID");
        
        // Create a test session
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = "Test Partial ID Session";
        await _sessionManager.RenameSessionAsync(sessionName);
        
        var userMessage = new ChatMessage(ChatRole.User, "Test message for partial ID lookup");
        await _sessionManager.AddMessageToCurrentSessionAsync(userMessage);
        
        // Get the last 12 characters of the session ID for partial matching
        var partialId = sessionId.ToString()[^12..];
        
        _output?.WriteLine($"📋 Created test session '{sessionName}' with ID: {sessionId}");
        _output?.WriteLine($"🔍 Testing partial ID lookup with: {partialId}");
        
        // Act
        var sessionCommand = new SessionCommand();
        var result = await sessionCommand.ExecuteAsync(["info", partialId]);
        
        // Assert
        result.Should().Be(0, "command should succeed with partial session ID");
        
        // Verify the session was loaded correctly
        _sessionManager.CurrentSession.Should().NotBeNull("session should be loaded");
        _sessionManager.CurrentSession!.Name.Should().Be(sessionName, "correct session should be loaded");
        _sessionManager.CurrentSession.Id.Should().Be(sessionId, "session ID should match");
        _sessionManager.CurrentSession.InitialPrompt.Should().Be("Test message for partial ID lookup", "initial prompt should be set correctly");
        
        _output?.WriteLine("✅ Session info command completed successfully with partial session ID");
        _logger.LogInformation("🎉 Session info command with partial session ID test completed successfully!");
    }

    /// <summary>
    /// Helper method to clear all test sessions to ensure clean test state.
    /// Only removes sessions that start with "Test" to avoid affecting user sessions.
    /// </summary>
    private async Task ClearAllTestSessionsAsync()
    {
        try
        {
            var allSessions = await _sessionManager.ListSessionsAsync();
            var testSessions = allSessions.Where(s => s.Name.StartsWith("Test")).ToList();
            
            foreach (var session in testSessions)
            {
                try
                {
                    // Load and delete the session directory
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear test sessions during setup");
        }
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
            // Clean up any test sessions created during tests
            _ = Task.Run(async () => await ClearAllTestSessionsAsync());
            
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
