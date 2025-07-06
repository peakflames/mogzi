using Mogzi.PawPrints;
using Xunit.Abstractions;

namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance test for complete chat workflow.
/// Tests the full pipeline from application startup through user interactions to AI responses.
/// Uses real ChatClient with "testing" profile and captures static stdout content for verification.
/// Follows no-mocking approach with real service configuration and DI container.
/// </summary>
public class ChatWorkflowAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly ILogger<ChatWorkflowAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public ChatWorkflowAcceptanceTests(ITestOutputHelper output)
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
        services.AddSingleton<ILogger<ChatWorkflowAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<ChatWorkflowAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)_serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _keyboardHandler = _serviceProvider.GetRequiredService<AdvancedKeyboardHandler>();
        _logger = _serviceProvider.GetRequiredService<ILogger<ChatWorkflowAcceptanceTests>>();
        
        _logger.LogInformation("ChatWorkflowAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task CompleteJokeConversationWorkflow_ShowsExpectedSequentialOrder()
    {
        // Arrange
        _output?.WriteLine("🚀 Starting complete chat workflow acceptance test");
        _logger.LogInformation("🚀 Starting complete chat workflow acceptance test");
        
        // Initialize the TUI application components
        await InitializeApplicationAsync();
        _output?.WriteLine("✅ TUI application initialized successfully");
        _logger.LogInformation("✅ TUI application initialized successfully");
        
        // Act & Assert - Complete workflow with progressive feedback
        
        // 1. Verify Welcome Screen
        VerifyWelcomeScreenDisplayed();
        _output?.WriteLine("✅ Welcome screen displayed correctly");
        _logger.LogInformation("✅ Welcome screen displayed correctly");
        
        // 2. First User Input: "tell me a joke"
        _output?.WriteLine("📝 Testing first user input: 'tell me a joke'");
        _logger.LogInformation("📝 Testing first user input: 'tell me a joke'");
        await SimulateCompleteUserInputAsync("tell me a joke");
        VerifyUserMessageDisplayed("tell me a joke", messageIndex: 1);
        _output?.WriteLine("✅ First user message captured and displayed");
        _logger.LogInformation("✅ First user message captured and displayed");
        
        await WaitForAssistantResponseAsync();
        VerifyAssistantResponseDisplayed(messageIndex: 2);
        _output?.WriteLine("✅ First assistant response received and displayed");
        _logger.LogInformation("✅ First assistant response received and displayed");
        
        // 3. Second User Input: "tell me another joke"
        _output?.WriteLine("📝 Testing second user input: 'tell me another joke'");
        _logger.LogInformation("📝 Testing second user input: 'tell me another joke'");
        await SimulateCompleteUserInputAsync("tell me another joke");
        VerifyUserMessageDisplayed("tell me another joke", messageIndex: 3);
        _output?.WriteLine("✅ Second user message captured and displayed");
        _logger.LogInformation("✅ Second user message captured and displayed");
        
        await WaitForAssistantResponseAsync();
        VerifyAssistantResponseDisplayed(messageIndex: 4);
        _output?.WriteLine("✅ Second assistant response received and displayed");
        _logger.LogInformation("✅ Second assistant response received and displayed");
        
        // 4. Verify Complete Sequential Order
        VerifyMessageSequentialOrder();
        _output?.WriteLine("✅ Message sequential order verified correctly");
        _logger.LogInformation("✅ Message sequential order verified correctly");
        
        _output?.WriteLine("🎉 Chat workflow acceptance test completed successfully - ALL CHECKS PASSED!");
        _logger.LogInformation("🎉 Chat workflow acceptance test completed successfully - ALL CHECKS PASSED!");
    }

    /// <summary>
    /// Initializes the TUI application components and displays welcome screen.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for workflow test");
        
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
        
        _logger.LogTrace("TUI application initialized successfully");
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
        
        // 2. Initialize AI operation context (required for state transition)
        _tuiContext.AiOperationCts = new CancellationTokenSource();
        _tuiContext.AiOperationStartTime = DateTime.Now;
        
        // 3. Press Enter to submit
        var enterKeyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);
        await _stateManager.HandleKeyPressAsync(enterEvent);
        
        // 4. Wait for AI processing to complete (will transition to Thinking then back to Input)
        await WaitForAssistantResponseAsync();
        
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
        
        var enterKeyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);
        await _stateManager.HandleKeyPressAsync(enterEvent);
        
        _logger.LogTrace("Enter key press completed");
    }

    /// <summary>
    /// Tries to wait for state transition with a timeout, returning success status.
    /// </summary>
    private async Task<bool> TryWaitForStateTransitionAsync(ChatState expectedState, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        
        while (_stateManager.CurrentStateType != expectedState && 
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }
        
        return _stateManager.CurrentStateType == expectedState;
    }

    /// <summary>
    /// Tries to wait for assistant response with a timeout, returning success status.
    /// </summary>
    private async Task<bool> TryWaitForAssistantResponseAsync(TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        
        while (_stateManager.CurrentStateType != ChatState.Input && 
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(100);
        }
        
        return _stateManager.CurrentStateType == ChatState.Input;
    }

    /// <summary>
    /// Verifies that the input context contains the expected text.
    /// </summary>
    private void VerifyInputContextContainsText(string expectedText)
    {
        _logger.LogTrace("Verifying input context contains: '{ExpectedText}'", expectedText);
        
        _tuiContext.InputContext.CurrentInput.Should().Be(expectedText,
            $"input context should contain '{expectedText}'");
        
        _logger.LogTrace("Input context verification completed");
    }

    /// <summary>
    /// Verifies that the test infrastructure is working correctly.
    /// </summary>
    private void VerifyTestInfrastructureWorks()
    {
        _logger.LogTrace("Verifying test infrastructure");
        
        // Verify we have captured some static content
        _testTerminal.StaticContent.Should().HaveCountGreaterThan(0,
            "test terminal should have captured static content");
        
        // Verify service provider is working
        _serviceProvider.Should().NotBeNull("service provider should be available");
        _tuiContext.Should().NotBeNull("TUI context should be available");
        _stateManager.Should().NotBeNull("state manager should be available");
        
        _logger.LogTrace("Test infrastructure verification completed");
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

    /// <summary>
    /// Waits for the application to transition to the specified state.
    /// </summary>
    private async Task WaitForStateTransitionAsync(ChatState expectedState)
    {
        var timeout = TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;
        
        while (_stateManager.CurrentStateType != expectedState && 
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(50);
        }
        
        _stateManager.CurrentStateType.Should().Be(expectedState,
            $"should transition to {expectedState} state within timeout");
    }

    /// <summary>
    /// Waits for AI processing to complete and return to Input state.
    /// </summary>
    private async Task WaitForAssistantResponseAsync()
    {
        _logger.LogTrace("Waiting for assistant response to complete");
        
        // Wait for AI processing to complete and return to Input state
        var timeout = TimeSpan.FromSeconds(30); // Reasonable timeout for real AI call
        var startTime = DateTime.UtcNow;
        
        while (_stateManager.CurrentStateType != ChatState.Input && 
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(100);
        }
        
        _stateManager.CurrentStateType.Should().Be(ChatState.Input,
            "should return to input state after assistant response");
        
        _logger.LogTrace("Assistant response completed, returned to input state");
    }

    /// <summary>
    /// Verifies that the welcome screen is displayed on startup.
    /// </summary>
    private void VerifyWelcomeScreenDisplayed()
    {
        _logger.LogTrace("Verifying welcome screen display");
        
        var welcomeEntries = _testTerminal.StaticContent
            .Where(entry => entry.Content.Contains("MOGZI", StringComparison.OrdinalIgnoreCase) || 
                           entry.Content.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
                           entry.Content.Contains("AI-powered", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        welcomeEntries.Should().HaveCountGreaterThan(0, 
            "welcome screen should be displayed on startup");
        
        _logger.LogTrace("Welcome screen verification completed");
    }

    /// <summary>
    /// Verifies that a user message appears in the static content.
    /// </summary>
    private void VerifyUserMessageDisplayed(string userInput, int messageIndex)
    {
        _logger.LogTrace("Verifying user message display: '{UserInput}' at index {MessageIndex}", userInput, messageIndex);
        
        var userEntries = _testTerminal.StaticContent
            .Where(entry => IsUserMessage(entry.Content) && 
                           entry.Content.Contains(userInput, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        userEntries.Should().HaveCountGreaterThan(0,
            $"user message '{userInput}' should appear in static content");
        
        _logger.LogTrace("User message verification completed");
    }

    /// <summary>
    /// Verifies that an assistant response appears in the static content.
    /// </summary>
    private void VerifyAssistantResponseDisplayed(int messageIndex)
    {
        _logger.LogTrace("Verifying assistant response display at index {MessageIndex}", messageIndex);
        
        var assistantEntries = _testTerminal.StaticContent
            .Where(entry => IsAssistantMessage(entry.Content))
            .ToList();
        
        // Don't check content - just verify an assistant message exists
        var expectedAssistantCount = messageIndex / 2;
        assistantEntries.Should().HaveCountGreaterOrEqualTo(expectedAssistantCount,
            "assistant response should appear in static content");
        
        _logger.LogTrace("Assistant response verification completed");
    }

    /// <summary>
    /// Verifies that messages appear in the correct sequential order.
    /// </summary>
    private void VerifyMessageSequentialOrder()
    {
        _logger.LogTrace("Verifying message sequential order");
        
        // Get distinct user messages (non-streaming)
        var userMessages = _testTerminal.StaticContent
            .Where(entry => IsUserMessage(entry.Content))
            .OrderBy(entry => entry.Timestamp)
            .ToList();
        
        // Get final assistant messages (non-updatable ones, which are the completed responses)
        var assistantMessages = _testTerminal.StaticContent
            .Where(entry => IsAssistantMessage(entry.Content) && !entry.IsUpdatable)
            .OrderBy(entry => entry.Timestamp)
            .ToList();
        
        userMessages.Should().HaveCount(2, "should have 2 user messages");
        assistantMessages.Should().HaveCount(2, "should have 2 completed assistant responses");
        
        // Verify content of user messages
        userMessages[0].Content.Should().Contain("tell me a joke", "first user message should be about jokes");
        userMessages[1].Content.Should().Contain("tell me another joke", "second user message should be about another joke");
        
        // Verify assistant messages contain joke content
        assistantMessages[0].Content.Should().Contain("✦", "first assistant message should have assistant marker");
        assistantMessages[1].Content.Should().Contain("✦", "second assistant message should have assistant marker");
        
        // Verify chronological order: User1 -> Assistant1 -> User2 -> Assistant2
        userMessages[0].Timestamp.Should().BeBefore(assistantMessages[0].Timestamp, "first user message should come before first assistant response");
        assistantMessages[0].Timestamp.Should().BeBefore(userMessages[1].Timestamp, "first assistant response should come before second user message");
        userMessages[1].Timestamp.Should().BeBefore(assistantMessages[1].Timestamp, "second user message should come before second assistant response");
        
        _logger.LogTrace("Message sequential order verification completed");
    }

    /// <summary>
    /// Determines if content represents a user message.
    /// </summary>
    private static bool IsUserMessage(string content)
    {
        // Look for user message indicators (based on RenderMessage logic)
        return content.Contains(">") && !content.Contains("✦");
    }

    /// <summary>
    /// Determines if content represents an assistant message.
    /// </summary>
    private static bool IsAssistantMessage(string content)
    {
        // Look for assistant message indicators - specifically the ✦ symbol
        // Don't use "assistant" as it appears in the welcome screen
        return content.Contains("✦");
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
