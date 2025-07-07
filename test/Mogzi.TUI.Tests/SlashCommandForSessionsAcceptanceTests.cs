namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for session management slash command functionality.
/// Tests the complete session command pipeline including session listing, clearing, and renaming.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
public class SlashCommandForSessionsAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly SlashCommandProcessor _slashCommandProcessor;
    private readonly SessionManager _sessionManager;
    private readonly ChatClient _chatClient;
    private readonly ILogger<SlashCommandForSessionsAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public SlashCommandForSessionsAcceptanceTests(ITestOutputHelper output)
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
        services.AddSingleton<ILogger<SlashCommandForSessionsAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<SlashCommandForSessionsAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)_serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _slashCommandProcessor = _serviceProvider.GetRequiredService<SlashCommandProcessor>();
        _sessionManager = _serviceProvider.GetRequiredService<SessionManager>();
        _chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        _logger = _serviceProvider.GetRequiredService<ILogger<SlashCommandForSessionsAcceptanceTests>>();
        
        _logger.LogInformation("SessionSlashCommandAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task SessionClearCommand_ClearsCurrentSessionHistory_WithoutAiInteraction()
    {
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

    [Fact]
    public async Task SessionListCommand_ShowsAvailableSessions_AllowsUserSelection()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /session list command functionality");
        _logger.LogInformation("🚀 Testing /session list command functionality");
        
        await InitializeApplicationAsync();
        
        // Create multiple test sessions
        await _sessionManager.CreateNewSessionAsync();
        var session1Id = _sessionManager.CurrentSession!.Id;
        await _sessionManager.RenameSessionAsync("Test Session 1");
        
        await _sessionManager.CreateNewSessionAsync();
        var session2Id = _sessionManager.CurrentSession!.Id;
        await _sessionManager.RenameSessionAsync("Test Session 2");
        
        _output?.WriteLine($"📋 Created test sessions: {session1Id}, {session2Id}");
        
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
        sessionNames.Should().Contain("Test Session 1", "should include first test session");
        sessionNames.Should().Contain("Test Session 2", "should include second test session");
        _output?.WriteLine("✅ Session completion items populated correctly");
        
        // Assert 4: Verify dynamic content shows session selection panel
        var currentState = _stateManager.CurrentState;
        currentState.Should().NotBeNull("current state should not be null");
        var dynamicContent = currentState!.RenderDynamicContent(_tuiContext);
        var renderedText = GetRenderedText(dynamicContent);
        renderedText.Should().Contain("Select", "should show selection panel header");
        _output?.WriteLine("✅ Dynamic session selection panel rendered correctly");
        
        // Act 3: Navigate to "Test Session 1" option (if not already selected)
        var targetIndex = _tuiContext.InputContext.CompletionItems
            .FindIndex(item => item.Text == "Test Session 1");
        if (targetIndex > 0)
        {
            _output?.WriteLine($"⬇️ Navigating to 'Test Session 1' option (index {targetIndex})");
            for (int i = 0; i < targetIndex; i++)
            {
                await SimulateKeyPressAsync(ConsoleKey.DownArrow);
            }
        }
        
        // Assert 5: Correct session should be selected
        _tuiContext.InputContext.SelectedSuggestionIndex.Should().Be(targetIndex,
            "should select the 'Test Session 1' option");
        _output?.WriteLine("✅ Navigation to 'Test Session 1' option successful");
        
        // Act 4: Select the session with Enter
        _output?.WriteLine("⏎ Selecting 'Test Session 1' option");
        await SimulateEnterKeyAsync();
        
        // Assert 6: Should return to normal input state
        _tuiContext.InputContext.State.Should().Be(InputState.Normal,
            "should return to normal input state after selection");
        _output?.WriteLine("✅ Returned to normal input state");
        
        // Assert 7: Should load the selected session
        _sessionManager.CurrentSession.Should().NotBeNull("should have a current session");
        _sessionManager.CurrentSession!.Name.Should().Be("Test Session 1",
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

    [Fact]
    public async Task SessionRenameCommand_WithArgument_RenamesCurrentSession()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /session rename command with argument");
        _logger.LogInformation("🚀 Testing /session rename command with argument");
        
        await InitializeApplicationAsync();
        
        // Create a session to rename
        await _sessionManager.CreateNewSessionAsync();
        var originalName = _sessionManager.CurrentSession!.Name;
        var sessionId = _sessionManager.CurrentSession.Id;
        _output?.WriteLine($"📋 Created session '{originalName}' with ID: {sessionId}");
        
        // Act: Type /session rename "My New Session Name" and press Enter
        var newName = "My New Session Name";
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
            _app?.Dispose();
            _testTerminal?.Dispose();
            
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
