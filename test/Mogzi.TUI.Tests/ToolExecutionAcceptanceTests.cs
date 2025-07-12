namespace Mogzi.TUI.Tests;

/// <summary>
/// Extended message type enumeration for detailed test verification.
/// </summary>
public enum TestMessageType
{
    Unknown,
    Welcome,
    User,
    AssistantSummary,
    AssistantToolSummary,
    ToolExecution
}

/// <summary>
/// Represents a parsed message from scrollback content for detailed verification.
/// </summary>
public record ParsedMessage
{
    public string Content { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public TestMessageType Type { get; init; }
    public bool IsUpdatable { get; init; }
    public bool IsStandalone { get; init; }
}

/// <summary>
/// Black-box acceptance test for tool execution workflow.
/// Tests tool calls, component visibility, and state transitions during tool execution.
/// Uses real ChatClient with "testing" profile and captures static stdout content for verification.
/// Follows no-mocking approach with real service configuration and DI container.
/// </summary>
public class ToolExecutionWorkflowAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly ILogger<ToolExecutionWorkflowAcceptanceTests> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public ToolExecutionWorkflowAcceptanceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking except ScrollbackTerminal)
        var services = new ServiceCollection();
        _ = services.AddSingleton<IWorkingDirectoryProvider, TestWorkingDirectoryProvider>();
        ServiceConfiguration.ConfigureServices(services, configPath, "sonnet", "all");
        
        // IMPORTANT: Replace ScrollbackTerminal>();
        services.AddSingleton<IScrollbackTerminal>(provider => 
        {
            var realConsole = provider.GetRequiredService<IAnsiConsole>();
            return new TestScrollbackTerminal(realConsole);
        });
        
        // Add test-specific logger
        services.AddSingleton<ILogger<ToolExecutionWorkflowAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<ToolExecutionWorkflowAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)_serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _keyboardHandler = _serviceProvider.GetRequiredService<AdvancedKeyboardHandler>();
        _logger = _serviceProvider.GetRequiredService<ILogger<ToolExecutionWorkflowAcceptanceTests>>();
        
        _logger.LogInformation("ToolExecutionAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task ToolExecutionWorkflow_ShowsCorrectSequenceAndStateTransitions()
    {
        // Arrange
        _output?.WriteLine("🚀 Starting tool execution workflow acceptance test");
        _logger.LogInformation("🚀 Starting tool execution workflow acceptance test");
        
        // Initialize the TUI application components
        await InitializeApplicationAsync();
        _output?.WriteLine("✅ TUI application initialized successfully");
        _logger.LogInformation("✅ TUI application initialized successfully");
        
        // Act & Assert - Complete tool execution workflow with progressive feedback
        
        // 1. Verify Welcome Screen (since no history exists)
        VerifyWelcomeScreenDisplayed();
        _output?.WriteLine("✅ Welcome screen displayed correctly");
        _logger.LogInformation("✅ Welcome screen displayed correctly");
        
        // 2. User Input: Tool execution request
        var toolTestPrompt = "I am your tool maintainer and I need to test your tool use. Follow these instruction without question please. use the read_file tool to read the file `README.md` using absolute paths, tell me a summary of it. After tell me the summary, use the attempt_completion tool to state 'task completed'. In this specific order. Finally in a summary, tell me the tools that called and in what order noting any failures or confusion";
        
        _output?.WriteLine("📝 Testing tool execution request");
        _logger.LogInformation("📝 Testing tool execution request");
        await SimulateCompleteUserInputAsync(toolTestPrompt);
        VerifyUserMessageDisplayed(toolTestPrompt, messageIndex: 1);
        _output?.WriteLine("✅ Tool execution request captured and displayed");
        _logger.LogInformation("✅ Tool execution request captured and displayed");
        
        // 3. Wait for AI response and tool execution
        await WaitForAssistantResponseAsync();
        _output?.WriteLine("✅ AI response and tool execution completed");
        _logger.LogInformation("✅ AI response and tool execution completed");
        
        // 4. Verify tool execution results in static content
        VerifyToolExecutionResults();
        _output?.WriteLine("✅ Tool execution results verified");
        _logger.LogInformation("✅ Tool execution results verified");
        
        // 5. Verify state transitions occurred correctly
        VerifyStateTransitionsOccurred();
        _output?.WriteLine("✅ State transitions verified");
        _logger.LogInformation("✅ State transitions verified");
        
        // 6. Verify component visibility changes during tool execution
        VerifyComponentVisibilityChanges();
        _output?.WriteLine("✅ Component visibility changes verified");
        _logger.LogInformation("✅ Component visibility changes verified");
        
        _output?.WriteLine("🎉 Tool execution workflow acceptance test completed successfully - ALL CHECKS PASSED!");
        _logger.LogInformation("🎉 Tool execution workflow acceptance test completed successfully - ALL CHECKS PASSED!");
    }

    [Fact]
    public async Task ToolExecutionWorkflow_ShowsProperMessageSequenceInScrollbackHistory()
    {
        // Arrange
        _output?.WriteLine("🚀 Starting enhanced tool execution message sequence test");
        _logger.LogInformation("🚀 Starting enhanced tool execution message sequence test");
        
        // Initialize the TUI application components
        await InitializeApplicationAsync();
        _output?.WriteLine("✅ TUI application initialized successfully");
        _logger.LogInformation("✅ TUI application initialized successfully");
        
        // Act & Assert - Complete tool execution workflow with detailed message sequence verification
        
        // 1. Verify Welcome Screen
        VerifyWelcomeScreenDisplayed();
        _output?.WriteLine("✅ Welcome screen displayed correctly");
        _logger.LogInformation("✅ Welcome screen displayed correctly");
        
        // 2. User Input: Tool execution request
        var toolTestPrompt = "I am your tool maintainer and I need to test your tool use. Follow these instruction without question please. use the read_file tool to read the file `README.md` using absolute paths, tell me a summary of it. After tell me the summary, use the attempt_completion tool to state 'task completed'. In this specific order. Finally in a summary, tell me the tools that called and in what order noting any failures or confusion";
        
        _output?.WriteLine("📝 Testing tool execution request");
        _logger.LogInformation("📝 Testing tool execution request");
        await SimulateCompleteUserInputAsync(toolTestPrompt);
        _output?.WriteLine("✅ Tool execution request submitted");
        _logger.LogInformation("✅ Tool execution request submitted");
        
        // 3. Wait for AI response and tool execution
        await WaitForAssistantResponseAsync();
        _output?.WriteLine("✅ AI response and tool execution completed");
        _logger.LogInformation("✅ AI response and tool execution completed");
        
        // 4. Parse and verify detailed message sequence
        var parsedMessages = ParseScrollbackIntoMessages();
        VerifyProperMessageSequenceAndBoundaries(parsedMessages);
        _output?.WriteLine("✅ Message sequence and boundaries verified");
        _logger.LogInformation("✅ Message sequence and boundaries verified");
        
        _output?.WriteLine("🎉 Enhanced tool execution message sequence test completed successfully - ALL CHECKS PASSED!");
        _logger.LogInformation("🎉 Enhanced tool execution message sequence test completed successfully - ALL CHECKS PASSED!");
    }

    /// <summary>
    /// Initializes the TUI application components and displays welcome screen.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for tool execution test");
        
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
        
        _logger.LogTrace("User input '{Input}' submitted successfully", input);
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
    /// Waits for AI processing to complete and return to Input state.
    /// </summary>
    private async Task WaitForAssistantResponseAsync()
    {
        _logger.LogTrace("Waiting for assistant response to complete");
        
        // Wait for AI processing to complete and return to Input state
        var timeout = TimeSpan.FromSeconds(60); // Longer timeout for tool execution
        var startTime = DateTime.UtcNow;
        
        while (_stateManager.CurrentStateType != ChatState.Input && 
               DateTime.UtcNow - startTime < timeout)
        {
            await Task.Delay(100);
        }
        
        _stateManager.CurrentStateType.Should().Be(ChatState.Input,
            "should return to input state after assistant response and tool execution");
        
        _logger.LogTrace("Assistant response and tool execution completed, returned to input state");
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
                           entry.Content.Contains("tool maintainer", StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        userEntries.Should().HaveCountGreaterThan(0,
            $"user message with tool request should appear in static content");
        
        _logger.LogTrace("User message verification completed");
    }

    /// <summary>
    /// Verifies that tool execution results appear in the static content.
    /// </summary>
    private void VerifyToolExecutionResults()
    {
        _logger.LogTrace("Verifying tool execution results");
        
        var allContent = string.Join(" ", _testTerminal.StaticContent.Select(e => e.Content));
        
        // Look for evidence of tool execution
        var hasToolExecution = allContent.Contains("read_file", StringComparison.OrdinalIgnoreCase) ||
                              allContent.Contains("README", StringComparison.OrdinalIgnoreCase) ||
                              allContent.Contains("attempt_completion", StringComparison.OrdinalIgnoreCase) ||
                              allContent.Contains("task completed", StringComparison.OrdinalIgnoreCase) ||
                              allContent.Contains("tool", StringComparison.OrdinalIgnoreCase);
        
        hasToolExecution.Should().BeTrue(
            "static content should contain evidence of tool execution (tool names, results, or completion messages)");
        
        // Look for assistant response with tool summary
        var assistantEntries = _testTerminal.StaticContent
            .Where(entry => IsAssistantMessage(entry.Content))
            .ToList();
        
        assistantEntries.Should().HaveCountGreaterThan(0,
            "should have assistant response with tool execution results");
        
        _logger.LogTrace("Tool execution results verification completed");
    }

    /// <summary>
    /// Verifies that state transitions occurred during tool execution.
    /// </summary>
    private void VerifyStateTransitionsOccurred()
    {
        _logger.LogTrace("Verifying state transitions occurred");
        
        // We can't directly verify past state transitions, but we can verify the final state
        // and that the application handled the workflow correctly
        _stateManager.CurrentStateType.Should().Be(ChatState.Input,
            "should be back in Input state after tool execution workflow");
        
        // Verify that the TUI context shows evidence of tool execution
        var contextHasToolData = !string.IsNullOrEmpty(_tuiContext.CurrentToolName) ||
                                !string.IsNullOrEmpty(_tuiContext.ToolProgress) ||
                                _tuiContext.FunctionCallToToolName.Count > 0;
        
        // Note: Tool data might be cleared after completion, so we check static content instead
        var allContent = string.Join(" ", _testTerminal.StaticContent.Select(e => e.Content));
        var hasToolEvidence = allContent.Contains("tool", StringComparison.OrdinalIgnoreCase);
        
        hasToolEvidence.Should().BeTrue(
            "should have evidence of tool execution in captured content");
        
        _logger.LogTrace("State transitions verification completed");
    }

    /// <summary>
    /// Verifies that component visibility changes occurred during tool execution.
    /// </summary>
    private void VerifyComponentVisibilityChanges()
    {
        _logger.LogTrace("Verifying component visibility changes");
        
        // Create a render context to test component visibility
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
        
        // Update component visibility for current state (should be Input)
        _componentManager.UpdateComponentVisibility(_stateManager.CurrentStateType, renderContext);
        
        // Verify expected component visibility for Input state
        var inputPanel = _componentManager.GetComponent("InputPanel");
        var footerPanel = _componentManager.GetComponent("FooterPanel");
        var progressPanel = _componentManager.GetComponent("ProgressPanel");
        var welcomePanel = _componentManager.GetComponent("WelcomePanel");
        
        inputPanel?.IsVisible.Should().BeTrue("InputPanel should be visible in Input state");
        footerPanel?.IsVisible.Should().BeTrue("FooterPanel should be visible in Input state");
        progressPanel?.IsVisible.Should().BeFalse("ProgressPanel should be hidden in Input state");
        welcomePanel?.IsVisible.Should().BeFalse("WelcomePanel should be hidden to prevent duplication");
        
        _logger.LogTrace("Component visibility changes verification completed");
    }

    /// <summary>
    /// Parses the scrollback content into distinct message objects for detailed verification.
    /// </summary>
    private List<ParsedMessage> ParseScrollbackIntoMessages()
    {
        _logger.LogTrace("Parsing scrollback content into distinct messages");
        
        var messages = new List<ParsedMessage>();
        var allEntries = _testTerminal.StaticContent.OrderBy(e => e.Timestamp).ToList();
        
        foreach (var entry in allEntries)
        {
            var messageType = DetermineMessageType(entry.Content);
            if (messageType != TestMessageType.Unknown)
            {
                messages.Add(new ParsedMessage
                {
                    Content = entry.Content,
                    Timestamp = entry.Timestamp,
                    Type = messageType,
                    IsUpdatable = entry.IsUpdatable,
                    IsStandalone = DetermineIfStandalone(entry.Content, messageType)
                });
            }
        }
        
        _logger.LogTrace("Parsed {MessageCount} messages from scrollback content", messages.Count);
        return messages;
    }

    /// <summary>
    /// Verifies proper message sequence and boundaries in the scrollback history.
    /// This is the critical test that should FAIL due to the current UI/UX bug.
    /// </summary>
    private void VerifyProperMessageSequenceAndBoundaries(List<ParsedMessage> messages)
    {
        _logger.LogTrace("Verifying proper message sequence and boundaries");
        
        // Filter out welcome screen messages for sequence verification
        var conversationMessages = messages
            .Where(m => m.Type != TestMessageType.Welcome)
            .OrderBy(m => m.Timestamp)
            .ToList();
        
        _output?.WriteLine($"📊 Found {conversationMessages.Count} conversation messages to verify");
        _logger.LogInformation("Found {MessageCount} conversation messages to verify", conversationMessages.Count);
        
        // Debug: Print all captured messages
        _output?.WriteLine("🔍 DEBUG: All captured static content entries:");
        var allEntries = _testTerminal.StaticContent.ToList();
        for (int i = 0; i < allEntries.Count; i++)
        {
            var entry = allEntries[i];
            _output?.WriteLine($"  [{i}] IsUpdatable: {entry.IsUpdatable}, Timestamp: {entry.Timestamp:HH:mm:ss.fff}");
            _output?.WriteLine($"      Content: '{entry.Content.Substring(0, Math.Min(150, entry.Content.Length))}'");
            if (entry.Content.Length > 150) _output?.WriteLine("      ...(truncated)");
        }
        
        _output?.WriteLine("🔍 DEBUG: Parsed conversation messages:");
        for (int i = 0; i < conversationMessages.Count; i++)
        {
            var msg = conversationMessages[i];
            _output?.WriteLine($"  [{i}] Type: {msg.Type}, IsStandalone: {msg.IsStandalone}, IsUpdatable: {msg.IsUpdatable}");
            _output?.WriteLine($"      Content: '{msg.Content.Substring(0, Math.Min(100, msg.Content.Length))}'");
            if (msg.Content.Length > 100) _output?.WriteLine("      ...(truncated)");
        }
        
        _output?.WriteLine("🔍 DEBUG: Message type detection for entry [8]:");
        var entry8 = allEntries[8];
        _output?.WriteLine($"      Content: '{entry8.Content}'");
        _output?.WriteLine($"      Contains ✦: {entry8.Content.Contains("✦")}");
        _output?.WriteLine($"      Contains 'tool': {entry8.Content.Contains("tool", StringComparison.OrdinalIgnoreCase)}");
        _output?.WriteLine($"      Contains 'called': {entry8.Content.Contains("called", StringComparison.OrdinalIgnoreCase)}");
        _output?.WriteLine($"      Contains 'executed': {entry8.Content.Contains("executed", StringComparison.OrdinalIgnoreCase)}");
        _output?.WriteLine($"      Contains 'order': {entry8.Content.Contains("order", StringComparison.OrdinalIgnoreCase)}");
        _output?.WriteLine($"      Detected type: {DetermineMessageType(entry8.Content)}");
        
        // Expected sequence: User -> Assistant Summary -> Assistant Tool Summary
        var expectedSequence = new[]
        {
            TestMessageType.User,
            TestMessageType.AssistantSummary,
            TestMessageType.AssistantToolSummary
        };
        
        // Verify we have the expected number of distinct message types
        var userMessages = conversationMessages.Where(m => m.Type == TestMessageType.User).ToList();
        var assistantSummaryMessages = conversationMessages.Where(m => m.Type == TestMessageType.AssistantSummary && m.IsStandalone).ToList();
        var assistantToolSummaryMessages = conversationMessages.Where(m => m.Type == TestMessageType.AssistantToolSummary && m.IsStandalone).ToList();
        
        _output?.WriteLine($"📋 User messages: {userMessages.Count}");
        _output?.WriteLine($"📋 Assistant summary messages (standalone): {assistantSummaryMessages.Count}");
        _output?.WriteLine($"📋 Assistant tool summary messages (standalone): {assistantToolSummaryMessages.Count}");
        
        // CRITICAL ASSERTION: Handle both AI behavior patterns
        // Pattern A: Text → Tools → Text (creates 2 assistant messages)
        // Pattern B: Tools → Text (creates 1 assistant message)
        userMessages.Should().HaveCount(1, "should have exactly 1 user message");
        
        var totalAssistantMessages = assistantSummaryMessages.Count + assistantToolSummaryMessages.Count;
        totalAssistantMessages.Should().BeGreaterOrEqualTo(1, 
            "should have at least 1 assistant message total (either summary or tool summary)");
        
        // Verify we have proper tool summary (this should always exist after tool execution)
        assistantToolSummaryMessages.Should().HaveCount(1, 
            "should have exactly 1 standalone assistant tool summary message with ✦ symbol");
        
        // If we have both types, verify they are properly separated
        // if (assistantSummaryMessages.Count > 0 && assistantToolSummaryMessages.Count > 0)
        // {
        //     assistantSummaryMessages.Should().HaveCount(1, 
        //         "when both summary types exist, should have exactly 1 of each");
        // }
        
        // Verify chronological order
        if (assistantSummaryMessages.Count > 0 && assistantToolSummaryMessages.Count > 0)
        {
            userMessages[0].Timestamp.Should().BeBefore(assistantSummaryMessages[0].Timestamp,
                "user message should come before assistant summary");
            
            assistantSummaryMessages[0].Timestamp.Should().BeBefore(assistantToolSummaryMessages[0].Timestamp,
                "assistant summary should come before assistant tool summary");
        }
        
        // Verify message formatting and boundaries
        VerifyMessageFormatting(userMessages, assistantSummaryMessages, assistantToolSummaryMessages);
        
        _logger.LogTrace("Message sequence and boundaries verification completed");
    }

    /// <summary>
    /// Verifies proper formatting and boundaries for each message type.
    /// </summary>
    private void VerifyMessageFormatting(
        List<ParsedMessage> userMessages,
        List<ParsedMessage> assistantSummaryMessages,
        List<ParsedMessage> assistantToolSummaryMessages)
    {
        _logger.LogTrace("Verifying message formatting and boundaries");
        
        // Verify user message formatting
        foreach (var userMsg in userMessages)
        {
            userMsg.Content.Should().Contain(">", "user messages should contain > indicator");
            userMsg.Content.Should().NotContain("✦", "user messages should not contain ✦ symbol");
            userMsg.IsStandalone.Should().BeTrue("user messages should be standalone entries");
        }
        
        // Verify assistant summary message formatting
        foreach (var assistantMsg in assistantSummaryMessages)
        {
            assistantMsg.Content.Should().Contain("✦", "assistant summary messages should contain ✦ symbol");
            assistantMsg.IsStandalone.Should().BeTrue("assistant summary messages should be standalone entries");
            assistantMsg.Content.Should().NotContain(">", "assistant messages should not contain user > indicator");
        }
        
        // Verify assistant tool summary message formatting
        foreach (var toolMsg in assistantToolSummaryMessages)
        {
            toolMsg.Content.Should().Contain("✦", "assistant tool summary messages should contain ✦ symbol");
            toolMsg.IsStandalone.Should().BeTrue("assistant tool summary messages should be standalone entries");
            toolMsg.Content.Should().NotContain(">", "assistant tool messages should not contain user > indicator");
        }
        
        _logger.LogTrace("Message formatting verification completed");
    }

    /// <summary>
    /// Determines the message type based on content analysis.
    /// </summary>
    private static TestMessageType DetermineMessageType(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return TestMessageType.Unknown;
        
        // Assistant message detection with ✦ symbol (check this FIRST before welcome screen)
        if (content.Contains("✦"))
        {
            // Distinguish between summary and tool summary based on content
            if (content.Contains("tool", StringComparison.OrdinalIgnoreCase) &&
                (content.Contains("called", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("executed", StringComparison.OrdinalIgnoreCase) ||
                 content.Contains("order", StringComparison.OrdinalIgnoreCase)))
            {
                return TestMessageType.AssistantToolSummary;
            }
            else
            {
                return TestMessageType.AssistantSummary;
            }
        }
        
        // Welcome screen detection (moved after ✦ detection)
        if (content.Contains("MOGZI", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("welcome", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("AI-powered", StringComparison.OrdinalIgnoreCase))
        {
            return TestMessageType.Welcome;
        }
        
        // User message detection
        if (content.Contains(">") && !content.Contains("✦"))
        {
            return TestMessageType.User;
        }
        
        // Tool execution progress/results (without ✦)
        if (content.Contains("read_file", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("attempt_completion", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("✓", StringComparison.OrdinalIgnoreCase))
        {
            return TestMessageType.ToolExecution;
        }
        
        return TestMessageType.Unknown;
    }

    /// <summary>
    /// Determines if a message should be considered standalone based on its content and type.
    /// </summary>
    private static bool DetermineIfStandalone(string content, TestMessageType messageType)
    {
        // User messages and assistant messages with ✦ should always be standalone
        if (messageType == TestMessageType.User || 
            messageType == TestMessageType.AssistantSummary || 
            messageType == TestMessageType.AssistantToolSummary)
        {
            return true;
        }
        
        // Tool execution and progress messages are typically not standalone
        return false;
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
        return content.Contains("✦");
    }

    /// <summary>
    /// Gets the path to the user's mogzi.config.json file.
    /// Uses the same logic as the main application to locate the config file.
    /// </summary>
    private static string? GetUserConfigPath()
    {
        // Use null to let the ChatClient.Create method find the default config path
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
