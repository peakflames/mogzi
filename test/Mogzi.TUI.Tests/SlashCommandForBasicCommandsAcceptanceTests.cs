namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for slash command functionality.
/// Tests the complete slash command pipeline including dynamic panels, user selection, and application lifecycle.
/// Uses real service configuration and DI container with no mocking approach.
/// Focuses on feature parity with legacy implementation for failing commands: /tool-approvals, /exit, /quit, /help.
/// </summary>
public class SlashCommandForBasicCommandsAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly SlashCommandProcessor _slashCommandProcessor;
    private readonly UserSelectionManager _userSelectionManager;
    private readonly ChatClient _chatClient;
    private readonly ILogger<SlashCommandForBasicCommandsAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public SlashCommandForBasicCommandsAcceptanceTests(ITestOutputHelper output)
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
        services.AddSingleton<ILogger<SlashCommandForBasicCommandsAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<SlashCommandForBasicCommandsAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)_serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _keyboardHandler = _serviceProvider.GetRequiredService<AdvancedKeyboardHandler>();
        _slashCommandProcessor = _serviceProvider.GetRequiredService<SlashCommandProcessor>();
        _userSelectionManager = _serviceProvider.GetRequiredService<UserSelectionManager>();
        _chatClient = _serviceProvider.GetRequiredService<ChatClient>();
        _logger = _serviceProvider.GetRequiredService<ILogger<SlashCommandForBasicCommandsAcceptanceTests>>();
        
        _logger.LogInformation("SlashCommandAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task ToolApprovalsCommand_ShowsUserSelectionPanel_AndUpdatesConfig()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /tool-approvals command with dynamic user selection");
        _logger.LogInformation("🚀 Testing /tool-approvals command with dynamic user selection");
        
        await InitializeApplicationAsync();
        var originalToolApprovals = _chatClient.Config.ToolApprovals;
        _output?.WriteLine($"📋 Original tool approvals setting: {originalToolApprovals}");
        
        // Act 1: Type /tool-approvals
        _output?.WriteLine("📝 Typing '/tool-approvals'");
        await SimulateUserTypingAsync("/tool-approvals");
        
        // Assert 1: Should be in autocomplete state while typing slash commands
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
        
        // Assert 3: Should have completion items for tool approvals
        _tuiContext.InputContext.CompletionItems.Should().HaveCount(2,
            "should have 2 tool approval options");
        _tuiContext.InputContext.CompletionItems[0].Text.Should().Be("readonly",
            "first option should be readonly");
        _tuiContext.InputContext.CompletionItems[1].Text.Should().Be("all",
            "second option should be all");
        _output?.WriteLine("✅ Completion items populated correctly");
        
        // Assert 4: Verify dynamic content shows selection panel
        var currentState = _stateManager.CurrentState;
        currentState.Should().NotBeNull("current state should not be null");
        if (currentState is null)
        {
            Assert.Fail("current state should not be null");
        }
        var dynamicContent = currentState.RenderDynamicContent(_tuiContext);
        var renderedText = GetRenderedText(dynamicContent);
        renderedText.Should().Contain("Select an option", 
            "should show selection panel header");
        renderedText.Should().Contain("readonly", 
            "should show readonly option");
        renderedText.Should().Contain("all", 
            "should show all option");
        _output?.WriteLine("✅ Dynamic selection panel rendered correctly");
        
        // Act 3: Navigate to "all" option (down arrow)
        _output?.WriteLine("⬇️ Navigating to 'all' option");
        await SimulateKeyPressAsync(ConsoleKey.DownArrow);
        
        // Assert 5: Selection index should change
        _tuiContext.InputContext.SelectedSuggestionIndex.Should().Be(1,
            "should select the 'all' option");
        _output?.WriteLine("✅ Navigation to 'all' option successful");
        
        // Act 4: Select the option with Enter
        _output?.WriteLine("⏎ Selecting 'all' option");
        await SimulateEnterKeyAsync();
        
        // Assert 6: Should return to normal input state
        _tuiContext.InputContext.State.Should().Be(InputState.Normal,
            "should return to normal input state after selection");
        _output?.WriteLine("✅ Returned to normal input state");
        
        // Assert 7: Config should be updated
        _chatClient.Config.ToolApprovals.Should().Be("all",
            "tool approvals config should be updated to 'all'");
        _output?.WriteLine($"✅ Tool approvals updated to: {_chatClient.Config.ToolApprovals}");
        
        // Assert 8: Should NOT have sent message to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty(
            "should not create any chat messages for interactive commands");
        _output?.WriteLine("✅ No AI messages created (correct interactive behavior)");
        
        _output?.WriteLine("🎉 /tool-approvals command test completed successfully!");
        _logger.LogInformation("🎉 /tool-approvals command test completed successfully!");
    }

    [Fact]
    public async Task ExitCommand_TriggersApplicationShutdown()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /exit command functionality");
        _logger.LogInformation("🚀 Testing /exit command functionality");
        
        await InitializeApplicationAsync();
        
        var exitEventFired = false;
        _slashCommandProcessor.ExitRequested += () => {
            exitEventFired = true;
            _output?.WriteLine("🔥 EXIT EVENT FIRED!");
        };
        
        // Debug: Test slash command processor directly
        _output?.WriteLine("🔍 Testing slash command processor directly");
        var directResult = _slashCommandProcessor.TryProcessCommand("/exit", out var directOutput);
        _output?.WriteLine($"🔍 Direct processing result: {directResult}, output: {directOutput}");
        _output?.WriteLine($"🔍 Exit event fired after direct test: {exitEventFired}");
        
        // Reset for actual test
        exitEventFired = false;
        
        // Act 1: Type /exit and press Enter
        _output?.WriteLine("📝 Typing '/exit' and pressing Enter");
        await SimulateCompleteUserInputAsync("/exit");
        
        // Debug: Check what happened
        _output?.WriteLine($"🔍 Exit event fired after simulation: {exitEventFired}");
        _output?.WriteLine($"🔍 Final input state: '{_tuiContext.InputContext.CurrentInput}'");
        _output?.WriteLine($"🔍 Final state: {_stateManager.CurrentStateType}");
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        _output?.WriteLine($"🔍 Chat history count: {chatHistory.Count}");
        if (chatHistory.Any())
        {
            _output?.WriteLine($"🔍 Last message: {chatHistory.Last().Text}");
        }
        
        // Assert 1: Exit event should be fired
        exitEventFired.Should().BeTrue("exit event should be triggered");
        _output?.WriteLine("✅ Exit event fired correctly");
        
        // Assert 2: Should display exit message
        var exitEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("Goodbye", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("Exiting", StringComparison.OrdinalIgnoreCase))
            .ToList();
        exitEntries.Should().HaveCountGreaterThan(0, "should display exit message");
        _output?.WriteLine("✅ Exit message displayed");
        
        // Assert 3: Should NOT send to AI
        chatHistory.Should().BeEmpty("should not create chat messages for exit command");
        _output?.WriteLine("✅ No AI messages created");
        
        _output?.WriteLine("🎉 /exit command test completed successfully!");
        _logger.LogInformation("🎉 /exit command test completed successfully!");
    }

    [Fact]
    public async Task QuitCommand_TriggersApplicationShutdown()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /quit command functionality");
        _logger.LogInformation("🚀 Testing /quit command functionality");
        
        await InitializeApplicationAsync();
        
        var exitEventFired = false;
        _slashCommandProcessor.ExitRequested += () => exitEventFired = true;
        
        // Act: Type /quit and press Enter
        _output?.WriteLine("📝 Typing '/quit' and pressing Enter");
        await SimulateCompleteUserInputAsync("/quit");
        
        // Assert: Same behavior as /exit
        exitEventFired.Should().BeTrue("quit event should be triggered");
        _output?.WriteLine("✅ Quit event fired correctly");
        
        var exitEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("Goodbye", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("Exiting", StringComparison.OrdinalIgnoreCase))
            .ToList();
        exitEntries.Should().HaveCountGreaterThan(0, "should display exit message");
        _output?.WriteLine("✅ Exit message displayed");
        
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty("should not create chat messages for quit command");
        _output?.WriteLine("✅ No AI messages created");
        
        _output?.WriteLine("🎉 /quit command test completed successfully!");
        _logger.LogInformation("🎉 /quit command test completed successfully!");
    }

    [Fact]
    public async Task HelpCommand_DisplaysCommandTable_WithoutAiInteraction()
    {
        // Arrange
        _output?.WriteLine("🚀 Testing /help command functionality");
        _logger.LogInformation("🚀 Testing /help command functionality");
        
        await InitializeApplicationAsync();
        
        // Act: Type /help and press Enter
        _output?.WriteLine("📝 Typing '/help' and pressing Enter");
        await SimulateCompleteUserInputAsync("/help");
        
        // Assert 1: Should display help content
        var helpEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("Available Commands", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("/help", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("/exit", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("/status", StringComparison.OrdinalIgnoreCase))
            .ToList();
        helpEntries.Should().HaveCountGreaterThan(0, "should display help information");
        _output?.WriteLine("✅ Help content displayed");
        
        // Assert 2: Should display usage tips
        var tipEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("Tips", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("Ctrl+C", StringComparison.OrdinalIgnoreCase))
            .ToList();
        tipEntries.Should().HaveCountGreaterThan(0, "should display usage tips");
        _output?.WriteLine("✅ Usage tips displayed");
        
        // Assert 3: Should NOT send to AI
        var chatHistory = _tuiContext.HistoryManager.GetCurrentChatHistory();
        chatHistory.Should().BeEmpty("should not create chat messages for help command");
        _output?.WriteLine("✅ No AI messages created");
        
        _output?.WriteLine("🎉 /help command test completed successfully!");
        _logger.LogInformation("🎉 /help command test completed successfully!");
    }

    /// <summary>
    /// Initializes the TUI application components and displays welcome screen.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for slash command tests");
        
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
        
        // Simulate the welcome screen display (normally done in FlexColumnTuiApp.RunAsync)
        var welcomePanel = _serviceProvider.GetRequiredService<WelcomePanel>();
        var welcomeContent = welcomePanel.Render(renderContext);
        _testTerminal.WriteStatic(welcomeContent);
        
        _logger.LogTrace("TUI application initialized successfully for slash command tests");
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
        
        _output?.WriteLine($"🔍 About to call HandleKeyPressAsync with Enter key");
        
        // Add a temporary event handler to see if SubmitCurrentInput is called
        var originalInput = _tuiContext.InputContext.CurrentInput;
        await _stateManager.HandleKeyPressAsync(enterEvent);
        var newInput = _tuiContext.InputContext.CurrentInput;
        
        _output?.WriteLine($"🔍 HandleKeyPressAsync completed, event handled: {enterEvent.Handled}");
        _output?.WriteLine($"🔍 Input changed from '{originalInput}' to '{newInput}'");
        
        // Check if the input was cleared (which would indicate SubmitCurrentInput was called)
        if (string.IsNullOrEmpty(newInput))
        {
            _output?.WriteLine("✅ Input was cleared - SubmitCurrentInput was called");
        }
        else
        {
            _output?.WriteLine("❌ Input was NOT cleared - SubmitCurrentInput was NOT called");
        }
        
        // NOTE: Removed HandleCharacterTypedAsync call for Enter key
        // Real console applications typically don't generate character typed events for control keys like Enter
        // The Enter key should only trigger HandleKeyPressAsync
        
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
            _keyboardHandler?.Dispose();
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
