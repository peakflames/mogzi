namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for input continuation command functionality.
/// Tests the autocomplete → Tab/Enter → input continuation → final submission workflow.
/// Uses real service configuration and DI container with no mocking approach.
/// Follows the systems-first testing philosophy with complete user workflows.
/// </summary>
[Collection("Sequential_Session_Tests")]
public class InputContinuationAcceptanceTests : SessionTestBase
{
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly SlashCommandProcessor _slashCommandProcessor;

    public InputContinuationAcceptanceTests(ITestOutputHelper output)
        : base(output, nameof(InputContinuationAcceptanceTests))
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

        _logger.LogInformation("InputContinuationAcceptanceTests initialized with shared SessionManager instance");
    }

    [Fact]
    public async Task SessionRenameCommand_AutocompleteToInputContinuation_CompletesWorkflow()
    {
        // TOR-5.3.10 - Enhanced UX for session rename with input continuation pattern
        // Arrange
        _output?.WriteLine("🚀 Testing /session rename autocomplete → input continuation → submission workflow");
        _logger.LogInformation("🚀 Testing /session rename input continuation pattern");

        await InitializeApplicationAsync();

        // Create a session to rename
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var originalName = _sessionManager.CurrentSession!.Name;
        var sessionId = _sessionManager.CurrentSession.Id;
        _output?.WriteLine($"📋 Created session '{originalName}' with ID: {sessionId}");

        // Act 1: Type partial command to trigger autocomplete
        _output?.WriteLine("📝 Step 1: Typing '/session r' to trigger autocomplete");
        await SimulateUserTypingAsync("/session r");

        // Assert 1: Should be in autocomplete state
        _tuiContext.InputContext.State.Should().Be(InputState.Autocomplete,
            "should be in autocomplete state while typing slash commands");
        _output?.WriteLine("✅ Confirmed autocomplete state");

        // Assert 2: Should have /session rename suggestion
        var suggestions = _tuiContext.InputContext.Suggestions;
        suggestions.Should().Contain("/session rename", "should suggest /session rename command");
        _output?.WriteLine("✅ /session rename suggestion available");

        // Act 2: Accept autocomplete suggestion with Tab
        _output?.WriteLine("📝 Step 2: Pressing Tab to accept '/session rename' suggestion");
        await SimulateTabKeyAsync();

        // Assert 3: Should populate input field with command + space for continuation
        _tuiContext.InputContext.CurrentInput.Should().Be("/session rename ",
            "should populate input field with command and trailing space for continuation");
        _tuiContext.InputContext.CursorPosition.Should().Be("/session rename ".Length,
            "cursor should be positioned after the trailing space");
        _tuiContext.InputContext.State.Should().Be(InputState.Normal,
            "should return to normal input state for continued typing");
        _output?.WriteLine("✅ Input field populated with '/session rename ' for continuation");

        // Act 3: Type the new session name
        var newName = $"Test-Renamed-{sessionGuid}";
        _output?.WriteLine($"📝 Step 3: Typing new session name '{newName}'");
        await SimulateUserTypingAsync(newName);

        // Assert 4: Should have complete command in input field
        var expectedCompleteCommand = $"/session rename {newName}";
        _tuiContext.InputContext.CurrentInput.Should().Be(expectedCompleteCommand,
            "should have complete command with new name in input field");
        _output?.WriteLine($"✅ Complete command assembled: '{expectedCompleteCommand}'");

        // Act 4: Submit the complete command
        _output?.WriteLine("📝 Step 4: Pressing Enter to submit complete command");
        await SimulateEnterKeyAsync();

        // Assert 5: Session should be renamed
        _sessionManager.CurrentSession.Should().NotBeNull("should have a current session");
        _sessionManager.CurrentSession!.Name.Should().Be(newName,
            "session should be renamed to the new name");
        _sessionManager.CurrentSession.Id.Should().Be(sessionId,
            "session ID should remain the same");
        _output?.WriteLine($"✅ Session renamed from '{originalName}' to '{newName}'");

        // Assert 6: Should display confirmation message
        var renameEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("renamed", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains(newName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        renameEntries.Should().HaveCountGreaterThan(0, "should display session rename confirmation");
        _output?.WriteLine("✅ Rename confirmation message displayed");

        // Assert 7: Should NOT send to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty("should not create chat messages for session rename command");
        _output?.WriteLine("✅ No AI messages created");

        // Assert 8: Input field should be cleared for next input
        _tuiContext.InputContext.CurrentInput.Should().BeEmpty("input field should be cleared after command execution");
        _output?.WriteLine("✅ Input field cleared for next input");

        _output?.WriteLine("🎉 Input continuation workflow test completed successfully!");
        _logger.LogInformation("🎉 Input continuation workflow test completed successfully!");
    }

    [Fact]
    public void SlashCommandProcessor_RequiresInputContinuation_DetectsCorrectCommands()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing SlashCommandProcessor.RequiresInputContinuation() method");
        _logger.LogInformation("🚀 Testing RequiresInputContinuation detection logic");

        // Act & Assert: Test various command inputs
        var testCases = new[]
        {
            // Input continuation commands
            ("/session rename", true, "should detect /session rename as input continuation command"),
            ("/session rename ", true, "should detect /session rename with space as input continuation command"),
            
            // Non-input continuation commands
            ("/session list", false, "should detect /session list as non-input continuation command"),
            ("/session clear", false, "should detect /session clear as non-input continuation command"),
            ("/help", false, "should detect /help as non-input continuation command"),
            ("/clear", false, "should detect /clear as non-input continuation command"),
            
            // Invalid inputs
            ("", false, "should return false for empty input"),
            ("not a command", false, "should return false for non-slash command"),
            ("/nonexistent", false, "should return false for non-existent command"),
        };

        foreach (var (input, expected, description) in testCases)
        {
            _output?.WriteLine($"🔍 Testing input: '{input}'");
            var result = _slashCommandProcessor.RequiresInputContinuation(input);
            result.Should().Be(expected, description);
            _output?.WriteLine($"✅ '{input}' → {result} (expected: {expected})");
        }

        _output?.WriteLine("🎉 RequiresInputContinuation detection test completed successfully!");
        _logger.LogInformation("🎉 RequiresInputContinuation detection test completed successfully!");
    }

    [Fact]
    public async Task InputContinuationPattern_WithEnterKey_WorksIdenticallyToTab()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing input continuation pattern with Enter key (alternative to Tab)");
        _logger.LogInformation("🚀 Testing Enter key for autocomplete acceptance");

        await InitializeApplicationAsync();

        // Create a session to rename
        var sessionGuid = Guid.NewGuid().ToString("N")[^8..];
        await _sessionManager.CreateNewSessionAsync();
        var originalName = _sessionManager.CurrentSession!.Name;
        var sessionId = _sessionManager.CurrentSession.Id;
        _output?.WriteLine($"📋 Created session '{originalName}' with ID: {sessionId}");

        // Act 1: Type partial command and accept with Enter instead of Tab
        _output?.WriteLine("📝 Typing '/session r' and pressing Enter to accept suggestion");
        await SimulateUserTypingAsync("/session r");
        await SimulateEnterKeyAsync(); // Use Enter instead of Tab

        // Assert: Should work identically to Tab
        _tuiContext.InputContext.CurrentInput.Should().Be("/session rename ",
            "Enter should work identically to Tab for input continuation commands");
        _tuiContext.InputContext.CursorPosition.Should().Be("/session rename ".Length,
            "cursor should be positioned correctly after Enter");
        _tuiContext.InputContext.State.Should().Be(InputState.Normal,
            "should be in normal state for continued typing");
        _output?.WriteLine("✅ Enter key works identically to Tab for input continuation");

        // Complete the workflow to ensure it works end-to-end
        var newName = $"Test-Enter-{sessionGuid}";
        await SimulateUserTypingAsync(newName);
        await SimulateEnterKeyAsync();

        _sessionManager.CurrentSession!.Name.Should().Be(newName,
            "complete workflow should work with Enter key");
        _output?.WriteLine($"✅ Complete workflow successful with Enter key: '{newName}'");

        _output?.WriteLine("🎉 Enter key input continuation test completed successfully!");
        _logger.LogInformation("🎉 Enter key input continuation test completed successfully!");
    }

    #region Helper Methods

    /// <summary>
    /// Initializes the TUI application components and displays welcome screen.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for input continuation tests");

        // Register state factories
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

        _logger.LogTrace("TUI application initialized successfully for input continuation tests");
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
    /// Simulates pressing the Tab key for autocomplete acceptance.
    /// </summary>
    private async Task SimulateTabKeyAsync()
    {
        _logger.LogTrace("Simulating Tab key press for autocomplete acceptance");
        _output?.WriteLine($"🔍 Current input before Tab: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state before Tab: {_stateManager.CurrentStateType}");

        var tabKeyInfo = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
        var tabEvent = new KeyPressEventArgs(tabKeyInfo);

        await _stateManager.HandleKeyPressAsync(tabEvent);

        _output?.WriteLine($"🔍 Current input after Tab: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state after Tab: {_stateManager.CurrentStateType}");

        _logger.LogTrace("Tab key press completed");
    }

    /// <summary>
    /// Simulates pressing the Enter key.
    /// </summary>
    private async Task SimulateEnterKeyAsync()
    {
        _logger.LogTrace("Simulating Enter key press");
        _output?.WriteLine($"🔍 Current input before Enter: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state before Enter: {_stateManager.CurrentStateType}");

        var enterKeyInfo = new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);

        await _stateManager.HandleKeyPressAsync(enterEvent);

        _output?.WriteLine($"🔍 Current input after Enter: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Current state after Enter: {_stateManager.CurrentStateType}");

        _logger.LogTrace("Enter key press completed");
    }

    /// <summary>
    /// Simulates a character being typed by the user.
    /// </summary>
    private async Task SimulateCharacterInputAsync(char character)
    {
        _logger.LogTrace("Simulating character input: '{Character}'", character);

        // Create a character typed event
        var characterEvent = new CharacterTypedEventArgs(character);

        // Process the character through the state manager
        await _stateManager.HandleCharacterTypedAsync(characterEvent);

        _logger.LogTrace("Character '{Character}' processed through state manager", character);
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
