namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for session command functionality (CLI and TUI).
/// Tests the complete session command pipeline including CLI commands and TUI slash commands.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
//[Collection("Sequential")] // Ensure tests run sequentially to avoid isolation issues
public class SessionCommandAcceptanceTests : SessionTestBase
{
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly SlashCommandProcessor _slashCommandProcessor;
    private readonly ChatClient _chatClient;

    public SessionCommandAcceptanceTests(ITestOutputHelper output) 
        : base(output, nameof(SessionCommandAcceptanceTests))
    {
        // Replace ScrollbackTerminal with test version to capture static content
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, GetUserConfigPath(), "testing", "readonly");
        
        // Override SessionManager to use the same instance from base class
        services.AddSingleton<SessionManager>(_ => _sessionManager);
        
        services.AddSingleton<IScrollbackTerminal>(provider => 
        {
            var realConsole = provider.GetRequiredService<IAnsiConsole>();
            return new TestScrollbackTerminal(realConsole);
        });
        
        var tuiServiceProvider = services.BuildServiceProvider();
        
        // Get TUI-specific services from the TUI service provider
        _app = tuiServiceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)tuiServiceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = tuiServiceProvider.GetRequiredService<ITuiContext>();
        _stateManager = tuiServiceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = tuiServiceProvider.GetRequiredService<ITuiComponentManager>();
        _slashCommandProcessor = tuiServiceProvider.GetRequiredService<SlashCommandProcessor>();
        _chatClient = tuiServiceProvider.GetRequiredService<ChatClient>();
        
        _logger.LogInformation("SessionCommandAcceptanceTests initialized with shared SessionManager instance");
    }

    #region CLI Command Tests

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

    [Fact(Skip = "Temporarily disabled - Runs fine in isolation but fails when run all together")]
    public async Task SessionListCommand_WithMultipleSessions_DisplaysSessionTable()
    {
        // TOR-5.3.2
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session list' with multiple sessions (process-level)");
        _logger.LogInformation("🚀 Testing 'mogzi session list' with multiple sessions (process-level)");
        
        // Clear any existing sessions first
        await ClearAllTestSessionsAsync();
        
        // Create test sessions with GUID-based names
        var session1Guid = Guid.NewGuid().ToString("N")[^8..];
        var session2Guid = Guid.NewGuid().ToString("N")[^8..];
        
        await _sessionManager.CreateNewSessionAsync();
        var session1Id = _sessionManager.CurrentSession!.Id;
        var session1Name = $"Test-{session1Guid}";
        await _sessionManager.RenameSessionAsync(session1Name);
        var testMessage1 = new ChatMessage(ChatRole.User, "Hello from session 1");
        await _sessionManager.AddMessageToCurrentSessionAsync(testMessage1);
        
        await _sessionManager.CreateNewSessionAsync();
        var session2Id = _sessionManager.CurrentSession!.Id;
        var session2Name = $"Test-{session2Guid}";
        await _sessionManager.RenameSessionAsync(session2Name);
        var testMessage2 = new ChatMessage(ChatRole.User, "Hello from session 2 with a longer message");
        await _sessionManager.AddMessageToCurrentSessionAsync(testMessage2);
        
        _output?.WriteLine($"📋 Created test sessions: {session1Name} ({session1Id}), {session2Name} ({session2Id})");
        
        // Act - Execute CLI command as separate process
        var (exitCode, output, error) = await ExecuteCliCommandAsync("session", "list");
        
        // Assert
        exitCode.Should().Be(0, "CLI command should succeed with sessions present"); // TOR-5.3.2
        error.Should().BeNullOrEmpty("CLI command should not produce errors");
        
        // Verify output contains our test sessions
        output.Should().Contain(session1Name, "output should contain first test session");
        output.Should().Contain(session2Name, "output should contain second test session");
        output.Should().Contain("Total:", "output should show session count");
        
        // Verify sessions exist by listing them again through SessionManager
        var allSessions = await _sessionManager.ListSessionsAsync(); // TOR-5.3.2
        var testSessions = allSessions.Where(s => s.Name.StartsWith("Test-")).ToList();
        testSessions.Should().HaveCount(2, "should have exactly 2 test sessions"); // TOR-5.3.2
        
        var session1 = testSessions.FirstOrDefault(s => s.Name == session1Name);
        var session2 = testSessions.FirstOrDefault(s => s.Name == session2Name);
        
        session1.Should().NotBeNull($"{session1Name} should exist");
        session2.Should().NotBeNull($"{session2Name} should exist");
        
        session1!.InitialPrompt.Should().Be("Hello from session 1", "session 1 should have correct initial prompt");
        session2!.InitialPrompt.Should().Be("Hello from session 2 with a longer message", "session 2 should have correct initial prompt");
        
        _output?.WriteLine("✅ CLI session list command completed successfully with multiple sessions");
        _logger.LogInformation("🎉 CLI session list command with multiple sessions test completed successfully!");
    }

    [Fact]
    public async Task SessionInfoCommand_WithValidSessionName_DisplaysSessionInformation()
    {
        // TOR-5.3.4
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with valid session name");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with valid session name");
        
        // Create a test session with GUID-based name
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = $"Test-{sessionGuid}";
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
        // TOR-5.3.4
        // Arrange
        _output?.WriteLine("🚀 Testing 'mogzi session info' with valid session ID");
        _logger.LogInformation("🚀 Testing 'mogzi session info' with valid session ID");
        
        // Create a test session with GUID-based name
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = $"Test-{sessionGuid}";
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
        // TOR-8.2
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
        
        // Create a test session with GUID-based name
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var sessionId = _sessionManager.CurrentSession!.Id;
        var sessionName = $"Test-{sessionGuid}";
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

    #endregion

    #region TUI Slash Command Tests

    [Fact]
    public async Task SessionClearCommand_ClearsCurrentSessionHistory_WithoutAiInteraction()
    {
        // TOR-5.3.5
        // Arrange
        _output?.WriteLine("🚀 Testing /session clear command functionality");
        _logger.LogInformation("🚀 Testing /session clear command functionality");
        
        await InitializeApplicationAsync();
        
        // Create a session with some history
        await _sessionManager.CreateNewSessionAsync();
        var testMessage = new ChatMessage(ChatRole.User, "test message for clearing");
        await _sessionManager.AddMessageToCurrentSessionAsync(testMessage);
        
        // Verify session has history before clearing
        var historyBeforeClear = _sessionManager.GetCurrentSessionMessages();
        historyBeforeClear.Should().HaveCount(1, "session should have one message before clearing");
        _output?.WriteLine($"📋 Session has {historyBeforeClear.Count} messages before clearing");
        
        // Act: Type /session clear and press Enter
        _output?.WriteLine("📝 Typing '/session clear' and pressing Enter");
        await SimulateCompleteUserInputAsync("/session clear");
        
        // Assert 1: Session history should be cleared
        var historyAfterClear = _sessionManager.GetCurrentSessionMessages();
        historyAfterClear.Should().BeEmpty("session history should be cleared");
        _output?.WriteLine("✅ Session history cleared successfully");
        
        // Assert 2: Should display confirmation message
        var clearEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("cleared", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("Session", StringComparison.OrdinalIgnoreCase))
            .ToList();
        clearEntries.Should().HaveCountGreaterThan(0, "should display session clear confirmation");
        _output?.WriteLine("✅ Clear confirmation message displayed");
        
        // Assert 3: Should NOT send to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty("should not create chat messages for session clear command");
        _output?.WriteLine("✅ No AI messages created");
        
        // Assert 4: Session should still exist but be empty
        _sessionManager.CurrentSession.Should().NotBeNull("session should still exist after clearing");
        _sessionManager.CurrentSession!.History.Should().BeEmpty("session history should be empty");
        _output?.WriteLine("✅ Session exists but history is empty");
        
        _output?.WriteLine("🎉 /session clear command test completed successfully!");
        _logger.LogInformation("🎉 /session clear command test completed successfully!");
    }

    [Fact(Skip = "Temporarily disabled - Runs fine in isolation but fails when run all together")]
    public async Task SessionListCommand_ShowsAvailableSessions_AllowsUserSelection()
    {
        // TOR-5.3.2
        // Arrange
        _output?.WriteLine("🚀 Testing /session list command functionality");
        _logger.LogInformation("🚀 Testing /session list command functionality");
        
        await InitializeApplicationAsync();
        
        // Clear any existing test sessions to ensure clean test state
        await ClearAllTestSessionsAsync();
        
        // Create multiple test sessions with unique GUID-based names (last 8 chars for UUIDv7)
        var session1Guid = Guid.NewGuid().ToString("N")[^8..]; // Last 8 chars of GUID
        var session2Guid = Guid.NewGuid().ToString("N")[^8..]; // Last 8 chars of GUID
        
        await _sessionManager.CreateNewSessionAsync();
        var session1Id = _sessionManager.CurrentSession!.Id;
        var session1Name = $"Test-{session1Guid}";
        await _sessionManager.RenameSessionAsync(session1Name);
        
        await _sessionManager.CreateNewSessionAsync();
        var session2Id = _sessionManager.CurrentSession!.Id;
        var session2Name = $"Test-{session2Guid}";
        await _sessionManager.RenameSessionAsync(session2Name);
        
        _output?.WriteLine($"📋 Created test sessions: {session1Name} ({session1Id}), {session2Name} ({session2Id})");
        
        // Act 1: Type /session list
        _output?.WriteLine("📝 Typing '/session list'");
        await SimulateUserTypingAsync("/session list");
        
        // Assert 1: Should be in autocomplete state while typing
        _tuiContext.InputContext.State.Should().Be(InputState.Autocomplete, 
            "should be in autocomplete state while typing slash commands");
        _output?.WriteLine("✅ Confirmed autocomplete state while typing slash command");
        
        // Act 2: Press Enter to trigger the command
        _output?.WriteLine("⏎ Pressing Enter to execute command");
        await SimulateEnterKeyAsync();
        
        // Assert 2: Should transition to UserSelection state
        _tuiContext.InputContext.State.Should().Be(InputState.UserSelection,
            "should transition to user selection state after pressing Enter");
        _output?.WriteLine("✅ Transitioned to UserSelection state");
        
        // Assert 3: Should have completion items for available sessions
        _tuiContext.InputContext.CompletionItems.Should().HaveCountGreaterOrEqualTo(2,
            "should have at least 2 session options");
        
        var sessionNames = _tuiContext.InputContext.CompletionItems.Select(item => item.Text).ToList();
        sessionNames.Should().Contain(session1Name, "should include first test session");
        sessionNames.Should().Contain(session2Name, "should include second test session");
        _output?.WriteLine("✅ Session completion items populated correctly");
        
        // Assert 4: Verify dynamic content shows session selection panel
        var currentState = _stateManager.CurrentState;
        currentState.Should().NotBeNull("current state should not be null");
        var dynamicContent = currentState!.RenderDynamicContent(_tuiContext);
        var renderedText = GetRenderedText(dynamicContent);
        renderedText.Should().Contain("Select", "should show selection panel header");
        _output?.WriteLine("✅ Dynamic session selection panel rendered correctly");
        
        // Act 3: Navigate to first test session option (find its actual index)
        var targetIndex = _tuiContext.InputContext.CompletionItems
            .FindIndex(item => item.Text == session1Name);
        
        targetIndex.Should().BeGreaterOrEqualTo(0, $"{session1Name} should be found in completion items");
        _output?.WriteLine($"🔍 Found '{session1Name}' at index {targetIndex}");
        
        // Navigate to the correct index (sessions are sorted by LastModifiedAt desc, so newer sessions appear first)
        var currentIndex = _tuiContext.InputContext.SelectedSuggestionIndex;
        _output?.WriteLine($"🔍 Current selection index: {currentIndex}, target index: {targetIndex}");
        
        if (targetIndex != currentIndex)
        {
            _output?.WriteLine($"⬇️ Navigating from index {currentIndex} to '{session1Name}' at index {targetIndex}");
            
            if (targetIndex > currentIndex)
            {
                // Navigate down
                for (int i = currentIndex; i < targetIndex; i++)
                {
                    await SimulateKeyPressAsync(ConsoleKey.DownArrow);
                }
            }
            else
            {
                // Navigate up
                for (int i = currentIndex; i > targetIndex; i--)
                {
                    await SimulateKeyPressAsync(ConsoleKey.UpArrow);
                }
            }
        }
        
        // Assert 5: Correct session should be selected
        _tuiContext.InputContext.SelectedSuggestionIndex.Should().Be(targetIndex,
            $"should select the '{session1Name}' option");
        _output?.WriteLine($"✅ Navigation to '{session1Name}' option successful (index {targetIndex})");
        
        // Act 4: Select the session with Enter
        _output?.WriteLine($"⏎ Selecting '{session1Name}' option");
        
        // Debug: Check what sessions are available before selection
        var sessionsBeforeSelection = await _sessionManager.ListSessionsAsync();
        var testSessionsBeforeSelection = sessionsBeforeSelection.Where(s => s.Name.StartsWith("Test-")).ToList();
        _output?.WriteLine($"🔍 Sessions available before selection:");
        for (int i = 0; i < testSessionsBeforeSelection.Count; i++)
        {
            _output?.WriteLine($"  [{i}] {testSessionsBeforeSelection[i].Name} (ID: {testSessionsBeforeSelection[i].Id})");
        }
        
        await SimulateEnterKeyAsync();
        
        // Assert 6: Should return to normal input state
        _tuiContext.InputContext.State.Should().Be(InputState.Normal,
            "should return to normal input state after selection");
        _output?.WriteLine("✅ Returned to normal input state");
        
        // Debug: Check what sessions are available after selection
        var sessionsAfterSelection = await _sessionManager.ListSessionsAsync();
        var testSessionsAfterSelection = sessionsAfterSelection.Where(s => s.Name.StartsWith("Test-")).ToList();
        _output?.WriteLine($"🔍 Sessions available after selection:");
        for (int i = 0; i < testSessionsAfterSelection.Count; i++)
        {
            _output?.WriteLine($"  [{i}] {testSessionsAfterSelection[i].Name} (ID: {testSessionsAfterSelection[i].Id})");
        }
        
        // Assert 7: Should load the selected session
        _sessionManager.CurrentSession.Should().NotBeNull("should have a current session");
        _output?.WriteLine($"🔍 Current session loaded: {_sessionManager.CurrentSession!.Name} (ID: {_sessionManager.CurrentSession.Id})");
        _sessionManager.CurrentSession!.Name.Should().Be(session1Name,
            "should load the selected session");
        _output?.WriteLine($"✅ Session loaded: {_sessionManager.CurrentSession.Name}");
        
        // Assert 8: Should NOT have sent message to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty(
            "should not create any chat messages for interactive commands");
        _output?.WriteLine("✅ No AI messages created (correct interactive behavior)");
        
        _output?.WriteLine("🎉 /session list command test completed successfully!");
        _logger.LogInformation("🎉 /session list command test completed successfully!");
    }

    [Fact(Skip = "Temporarily disabled - Runs fine in isolation but fails when run all together")]
    public async Task SessionRenameCommand_WithArgument_RenamesCurrentSession()
    {
        // TOR-5.3.10
        // Arrange
        _output?.WriteLine("🚀 Testing /session rename command with argument");
        _logger.LogInformation("🚀 Testing /session rename command with argument");
        
        await InitializeApplicationAsync();
        
        // Create a session to rename with GUID-based name
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var originalName = _sessionManager.CurrentSession!.Name;
        var sessionId = _sessionManager.CurrentSession.Id;
        _output?.WriteLine($"📋 Created session '{originalName}' with ID: {sessionId}");
        
        // Act: Type /session rename "My New Session Name" and press Enter
        var newName = $"Test-Renamed-{sessionGuid}";
        _output?.WriteLine($"📝 Typing '/session rename {newName}' and pressing Enter");
        await SimulateCompleteUserInputAsync($"/session rename {newName}");
        
        // Assert 1: Session should be renamed
        _sessionManager.CurrentSession.Should().NotBeNull("should have a current session");
        _sessionManager.CurrentSession!.Name.Should().Be(newName,
            "session should be renamed to the new name");
        _sessionManager.CurrentSession.Id.Should().Be(sessionId,
            "session ID should remain the same");
        _output?.WriteLine($"✅ Session renamed from '{originalName}' to '{newName}'");
        
        // Assert 2: Should display confirmation message
        var renameEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("renamed", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains(newName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        renameEntries.Should().HaveCountGreaterThan(0, "should display session rename confirmation");
        _output?.WriteLine("✅ Rename confirmation message displayed");
        
        // Assert 3: Should NOT send to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty("should not create chat messages for session rename command");
        _output?.WriteLine("✅ No AI messages created");
        
        // Assert 4: Session should be persisted with new name
        var allSessions = await _sessionManager.ListSessionsAsync();
        var renamedSession = allSessions.FirstOrDefault(s => s.Id == sessionId);
        renamedSession.Should().NotBeNull("renamed session should exist in session list");
        renamedSession!.Name.Should().Be(newName, "persisted session should have the new name");
        _output?.WriteLine("✅ Session rename persisted successfully");
        
        _output?.WriteLine("🎉 /session rename command test completed successfully!");
        _logger.LogInformation("🎉 /session rename command test completed successfully!");
    }

    #endregion

    #region CLI Helper Methods

    /// <summary>
    /// Executes a CLI command as a separate process and captures the output.
    /// This enables true process-level testing of CLI commands.
    /// </summary>
    private async Task<(int exitCode, string output, string error)> ExecuteCliCommandAsync(params string[] args)
    {
        try
        {
            // Find the mogzi executable
            var mogziPath = GetMogziExecutablePath();
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = mogziPath,
                Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            _output?.WriteLine($"🔧 Executing CLI command: {mogziPath} {processStartInfo.Arguments}");

            using var process = new Process { StartInfo = processStartInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            
            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            var exitCode = process.ExitCode;
            
            _output?.WriteLine($"🔧 CLI command completed with exit code: {exitCode}");
            if (!string.IsNullOrEmpty(output))
            {
                _output?.WriteLine($"📤 Output: {output}");
            }
            if (!string.IsNullOrEmpty(error))
            {
                _output?.WriteLine($"❌ Error: {error}");
            }
            
            return (exitCode, output, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute CLI command: {Args}", string.Join(" ", args));
            return (-1, "", ex.Message);
        }
    }

    /// <summary>
    /// Gets the path to the mogzi executable for testing.
    /// </summary>
    private string GetMogziExecutablePath()
    {
        // Look for the mogzi executable in the build output directory
        var currentDirectory = Directory.GetCurrentDirectory();
        _output?.WriteLine($"🔍 Current working directory: {currentDirectory}");
        
        var projectRoot = FindProjectRoot(currentDirectory);
        _output?.WriteLine($"🔍 Project root directory: {projectRoot}");
        
        // Try different possible paths for the mogzi executable
        var possiblePaths = new[]
        {
            Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Debug", "net9.0", "mogzi"),
            Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Debug", "net9.0", "mogzi.exe"),
            Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Release", "net9.0", "mogzi"),
            Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Release", "net9.0", "mogzi.exe"),
            Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Release", "net9.0", "linux-x64", "native", "mogzi"),
            "mogzi", // Try system PATH
            "mogzi.exe" // Try system PATH on Windows
        };

        _output?.WriteLine($"🔍 Checking {possiblePaths.Length} possible paths for mogzi executable:");
        foreach (var path in possiblePaths)
        {
            _output?.WriteLine($"🔍   Checking: {path}");
            if (File.Exists(path))
            {
                _output?.WriteLine($"🔍 Found mogzi executable at: {path}");
                return path;
            }
        }

        // If not found, try to build it first
        _output?.WriteLine("🔨 Mogzi executable not found, attempting to build...");
        var buildResult = BuildMogziExecutable(projectRoot);
        if (buildResult)
        {
            // Try the paths again after building
            foreach (var path in possiblePaths.Take(5)) // Only try the local build paths
            {
                _output?.WriteLine($"🔍   Re-checking after build: {path}");
                if (File.Exists(path))
                {
                    _output?.WriteLine($"🔍 Found mogzi executable after build at: {path}");
                    return path;
                }
            }
        }

        // List what files actually exist in the expected directory
        var debugBinPath = Path.Combine(projectRoot, "src", "Mogzi.TUI", "bin", "Debug", "net9.0");
        if (Directory.Exists(debugBinPath))
        {
            _output?.WriteLine($"🔍 Files in {debugBinPath}:");
            foreach (var file in Directory.GetFiles(debugBinPath))
            {
                _output?.WriteLine($"🔍   {file}");
            }
        }

        throw new FileNotFoundException($"Could not find mogzi executable. Searched in: {string.Join(", ", possiblePaths)}");
    }

    /// <summary>
    /// Finds the project root directory by looking for the .sln file.
    /// </summary>
    private static string FindProjectRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        
        // Fallback: if we can't find .sln, try to construct path based on known structure
        // Test runs from: /home/todd/peakflames/mogzi/test/Mogzi.TUI.Tests/bin/Debug/net9.0
        // Project root is: /home/todd/peakflames/mogzi
        var currentDir = new DirectoryInfo(startDirectory);
        
        // Look for "mogzi" directory in the path and use that as root
        while (currentDir != null)
        {
            if (currentDir.Name.Equals("mogzi", StringComparison.OrdinalIgnoreCase))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        
        throw new DirectoryNotFoundException($"Could not find project root directory with .sln file starting from: {startDirectory}");
    }

    /// <summary>
    /// Attempts to build the mogzi executable.
    /// </summary>
    private bool BuildMogziExecutable(string projectRoot)
    {
        try
        {
            _output?.WriteLine("🔨 Building mogzi executable...");
            
            var buildProcess = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build src/Mogzi.TUI/Mogzi.TUI.csproj -c Debug",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(buildProcess);
            if (process != null)
            {
                process.WaitForExit();
                var success = process.ExitCode == 0;
                _output?.WriteLine($"🔨 Build {(success ? "succeeded" : "failed")} with exit code: {process.ExitCode}");
                return success;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _output?.WriteLine($"🔨 Build failed with exception: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region TUI Helper Methods

    /// <summary>
    /// Initializes the TUI application components and displays welcome screen.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for session slash command tests");
        
        // Register state factories (normally done in FlexColumnTuiApp.RegisterStateFactories)
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
        
        // Initialize the state manager with the TUI context
        await _stateManager.InitializeAsync(_tuiContext);
        
        // Verify we're in the correct initial state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, 
            "application should start in Input state");
        
        // Create a render context for component initialization
        var renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();
        var themeInfo = _serviceProvider.GetRequiredService<IThemeInfo>();
        var renderContext = new RenderContext(
            _tuiContext,
            _stateManager.CurrentStateType,
            _logger,
            _serviceProvider,
            renderingUtilities,
            themeInfo
        );
        
        // Initialize components
        await _componentManager.InitializeComponentsAsync(renderContext);
        
        _logger.LogTrace("TUI application initialized successfully for session slash command tests");
    }

    /// <summary>
    /// Simulates complete user input including typing and pressing Enter.
    /// </summary>
    private async Task SimulateCompleteUserInputAsync(string input)
    {
        _logger.LogTrace("Simulating complete user input: '{Input}'", input);
        
        // 1. Type each character
        foreach (char character in input)
        {
            await SimulateCharacterInputAsync(character);
        }
        
        // 2. Press Enter to submit
        await SimulateEnterKeyAsync();
        
        _logger.LogTrace("User input '{Input}' submitted successfully", input);
    }

    /// <summary>
    /// Simulates typing characters without pressing Enter.
    /// </summary>
    private async Task SimulateUserTypingAsync(string input)
    {
        _logger.LogTrace("Simulating user typing: '{Input}'", input);
        
        foreach (char character in input)
        {
            await SimulateCharacterInputAsync(character);
        }
        
        _logger.LogTrace("User typing completed: '{Input}'", input);
    }

    /// <summary>
    /// Simulates pressing the Enter key.
    /// </summary>
    private async Task SimulateEnterKeyAsync()
    {
        _logger.LogTrace("Simulating Enter key press");
        _output?.WriteLine($"🔍 Current input before Enter: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state before Enter: {_stateManager.CurrentStateType}");
        
        // Use '\0' for the character since Enter is a control key, not a printable character
        var enterKeyInfo = new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);
        
        await _stateManager.HandleKeyPressAsync(enterEvent);
        
        _output?.WriteLine($"🔍 Current input after Enter: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state after Enter: {_stateManager.CurrentStateType}");
        
        _logger.LogTrace("Enter key press completed");
    }

    /// <summary>
    /// Simulates pressing a specific key.
    /// </summary>
    private async Task SimulateKeyPressAsync(ConsoleKey key)
    {
        _logger.LogTrace("Simulating key press: {Key}", key);
        
        var keyInfo = new ConsoleKeyInfo('\0', key, false, false, false);
        var keyEvent = new KeyPressEventArgs(keyInfo);
        await _stateManager.HandleKeyPressAsync(keyEvent);
        
        _logger.LogTrace("Key press completed: {Key}", key);
    }

    /// <summary>
    /// Simulates a character being typed by the user.
    /// </summary>
    private async Task SimulateCharacterInputAsync(char character)
    {
        _logger.LogTrace("Simulating character input: '{Character}'", character);
        _output?.WriteLine($"🔤 Typing character: '{character}' (ASCII: {(int)character})");
        _output?.WriteLine($"🔤 Input before: '{_tuiContext.InputContext.CurrentInput}'");
        
        // Create a character typed event
        var characterEvent = new CharacterTypedEventArgs(character);
        
        // Process the character through the state manager
        await _stateManager.HandleCharacterTypedAsync(characterEvent);
        
        _output?.WriteLine($"🔤 Input after: '{_tuiContext.InputContext.CurrentInput}'");
        _logger.LogTrace("Character '{Character}' processed through state manager", character);
    }

    /// <summary>
    /// Converts a Spectre.Console IRenderable to text for verification.
    /// Properly renders the component to capture actual text content.
    /// </summary>
    private static string GetRenderedText(IRenderable renderable)
    {
        try
        {
            // Create a StringWriter to capture the output
            var stringWriter = new StringWriter();
            
            // Create a test console that writes to our StringWriter
            var testConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No,
                ColorSystem = ColorSystemSupport.NoColors,
                Out = new AnsiConsoleOutput(stringWriter)
            });
            
            // Render the component to the test console
            testConsole.Write(renderable);
            
            // Get the rendered text from the StringWriter
            var renderedText = stringWriter.ToString();
            
            // Clean up any extra whitespace but preserve structure
            return renderedText.Trim();
        }
        catch (Exception ex)
        {
            // Fallback: try to extract text from known component types
            return ExtractTextFromComponent(renderable) ?? $"Error rendering: {ex.Message}";
        }
    }

    /// <summary>
    /// Fallback method to extract text from known Spectre.Console component types.
    /// </summary>
    private static string? ExtractTextFromComponent(IRenderable renderable)
    {
        return renderable switch
        {
            Panel panel => ExtractTextFromPanel(panel),
            Rows rows => ExtractTextFromRows(rows),
            Text text => text.ToString(),
            Markup markup => markup.ToString(),
            _ => renderable.ToString()
        };
    }

    /// <summary>
    /// Extracts text content from a Panel component.
    /// </summary>
    private static string ExtractTextFromPanel(Panel panel)
    {
        var content = new List<string>();
        
        // Add header if present
        var headerText = panel.Header?.Text;
        if (!string.IsNullOrEmpty(headerText))
        {
            content.Add(headerText);
        }
        
        // Try to extract content from the panel's child
        // This is a simplified approach - in reality, Panel content extraction is complex
        content.Add("Panel content");
        
        return string.Join("\n", content);
    }

    /// <summary>
    /// Extracts text content from a Rows component.
    /// </summary>
    private static string ExtractTextFromRows(Rows rows)
    {
        // This is a simplified approach
        // In reality, extracting text from Rows requires reflection or other techniques
        return "Rows content";
    }

    public override void Dispose()
    {
        try
        {
            _app?.Dispose();
            _testTerminal?.Dispose();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during TUI-specific test cleanup");
        }
        
        // Call base class dispose
        base.Dispose();
    }

    #endregion
}
