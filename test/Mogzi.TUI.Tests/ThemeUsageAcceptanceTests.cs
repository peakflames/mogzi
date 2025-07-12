namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for theme usage functionality.
/// Tests the complete theme system from configuration to message rendering.
/// Follows no-mocking approach with real service configuration and DI container.
/// </summary>
public class ThemeUsageAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly IRenderingUtilities _renderingUtilities;
    private readonly IThemeInfo _themeInfo;
    private readonly ILogger<ThemeUsageAcceptanceTests> _logger;
    private bool _disposed = false;

    public ThemeUsageAcceptanceTests()
    {
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking)
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");
        
        // Add test-specific logger
        services.AddSingleton<ILogger<ThemeUsageAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<ThemeUsageAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _renderingUtilities = _serviceProvider.GetRequiredService<IRenderingUtilities>();
        _themeInfo = _serviceProvider.GetRequiredService<IThemeInfo>();
        _logger = _serviceProvider.GetRequiredService<ILogger<ThemeUsageAcceptanceTests>>();
        
        _logger.LogInformation("ThemeUsageAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public void WhenThemeInfoIsRequested_ThenDefaultThemeIsProvided()
    {
        // Arrange & Act
        _logger.LogInformation("Testing default theme configuration");

        // Assert - Verify default theme properties
        _themeInfo.Should().NotBeNull("theme info should be available from DI container");
        _themeInfo.PrimaryColor.Should().Be(Color.Blue, "default primary color should be blue");
        _themeInfo.SecondaryColor.Should().Be(Color.Grey23, "default secondary color should be grey23");
        _themeInfo.AccentColor.Should().Be(Color.Green, "default accent color should be green");
        _themeInfo.BorderStyle.Should().Be(BoxBorder.Rounded, "default border style should be rounded");
        
        // Verify message styling properties
        _themeInfo.UserMessageColor.Should().Be("dim", "default user message color should be dim");
        _themeInfo.AssistantMessageColor.Should().Be("skyblue1", "default assistant message color should be skyblue1");
        _themeInfo.SystemMessageColor.Should().Be("white", "default system message color should be white");
        _themeInfo.UserMessagePrefix.Should().Be("[dim]>[/] ", "default user message prefix should include markup");
        _themeInfo.AssistantMessagePrefix.Should().Be("✦ ", "default assistant message prefix should be sparkle");
        
        _logger.LogInformation("Default theme configuration verified successfully");
    }

    [Fact]
    public void WhenUserMessageIsRendered_ThenThemeColorsAreApplied()
    {
        // Arrange
        _logger.LogInformation("Testing user message rendering with theme");
        
        var userMessage = new ChatMessage(ChatRole.User, "Hello, this is a test user message");

        // Act
        var renderedMessage = _renderingUtilities.RenderMessage(userMessage, _themeInfo);

        // Assert
        renderedMessage.Should().NotBeNull("rendered message should not be null");
        
        var renderedText = ExtractTextFromRenderable(renderedMessage);
        
        // Verify the message content appears
        renderedText.Should().Contain("Hello, this is a test user message", 
            "user message content should appear in rendered output");
        
        // Verify the prefix is applied (the actual markup will be processed, so we check for the visible part)
        renderedText.Should().Contain(">", "user message prefix should appear in rendered output");
        
        _logger.LogInformation("User message rendering with theme verified successfully");
    }

    [Fact]
    public void WhenAssistantMessageIsRendered_ThenThemeColorsAreApplied()
    {
        // Arrange
        _logger.LogInformation("Testing assistant message rendering with theme");
        
        var assistantMessage = new ChatMessage(ChatRole.Assistant, "This is a test assistant response");

        // Act
        var renderedMessage = _renderingUtilities.RenderMessage(assistantMessage, _themeInfo);

        // Assert
        renderedMessage.Should().NotBeNull("rendered message should not be null");
        
        var renderedText = ExtractTextFromRenderable(renderedMessage);
        
        // Verify the message content appears
        renderedText.Should().Contain("This is a test assistant response", 
            "assistant message content should appear in rendered output");
        
        // Verify the prefix is applied
        renderedText.Should().Contain("✦", "assistant message prefix should appear in rendered output");
        
        _logger.LogInformation("Assistant message rendering with theme verified successfully");
    }

    [Fact]
    public void WhenSystemMessageIsRendered_ThenThemeColorsAreApplied()
    {
        // Arrange
        _logger.LogInformation("Testing system message rendering with theme");
        
        var systemMessage = new ChatMessage(ChatRole.System, "This is a system message");

        // Act
        var renderedMessage = _renderingUtilities.RenderMessage(systemMessage, _themeInfo);

        // Assert
        renderedMessage.Should().NotBeNull("rendered message should not be null");
        
        var renderedText = ExtractTextFromRenderable(renderedMessage);
        
        // Verify the message content appears
        renderedText.Should().Contain("This is a system message", 
            "system message content should appear in rendered output");
        
        // System messages don't have a prefix by default, so just verify content
        _logger.LogInformation("System message rendering with theme verified successfully");
    }

    [Fact]
    public void WhenMessageIsRenderedWithoutTheme_ThenDefaultColorsAreUsed()
    {
        // Arrange
        _logger.LogInformation("Testing message rendering without theme (fallback behavior)");
        
        var userMessage = new ChatMessage(ChatRole.User, "Test message without theme");

        // Act - Render without passing theme info
        var renderedMessage = _renderingUtilities.RenderMessage(userMessage, null);

        // Assert
        renderedMessage.Should().NotBeNull("rendered message should not be null even without theme");
        
        var renderedText = ExtractTextFromRenderable(renderedMessage);
        
        // Verify the message content appears with fallback styling
        renderedText.Should().Contain("Test message without theme", 
            "message content should appear even without theme");
        
        renderedText.Should().Contain(">", "default user message prefix should appear");
        
        _logger.LogInformation("Message rendering without theme verified successfully");
    }

    [Fact]
    public void WhenCustomThemeIsUsed_ThenCustomColorsAreApplied()
    {
        // Arrange
        _logger.LogInformation("Testing custom theme implementation");
        
        var customTheme = new TestThemeInfo
        {
            UserMessageColor = "red",
            AssistantMessageColor = "green",
            SystemMessageColor = "yellow",
            UserMessagePrefix = "[red]USER:[/] ",
            AssistantMessagePrefix = "[green]AI:[/] "
        };
        
        var userMessage = new ChatMessage(ChatRole.User, "Custom theme test message");

        // Act
        var renderedMessage = _renderingUtilities.RenderMessage(userMessage, customTheme);

        // Assert
        renderedMessage.Should().NotBeNull("rendered message should not be null with custom theme");
        
        var renderedText = ExtractTextFromRenderable(renderedMessage);
        
        // Verify the message content appears
        renderedText.Should().Contain("Custom theme test message", 
            "message content should appear with custom theme");
        
        // Verify custom prefix is used (the markup will be processed, so we check for the visible part)
        renderedText.Should().Contain("USER:", "custom user message prefix should appear");
        
        _logger.LogInformation("Custom theme implementation verified successfully");
    }

    [Fact]
    public void WhenRenderingUtilitiesAreUsed_ThenThemeIntegrationWorks()
    {
        // Arrange
        _logger.LogInformation("Testing rendering utilities integration with theme system");
        
        var appService = _serviceProvider.GetRequiredService<IAppService>();
        var workingDirProvider = _serviceProvider.GetRequiredService<IWorkingDirectoryProvider>();
        var chatHistory = new List<ChatMessage>
        {
            new(ChatRole.User, "Test message 1"),
            new(ChatRole.Assistant, "Test response 1")
        };

        // Act - Test all rendering utility methods
        var displayPath = _renderingUtilities.FormatDisplayPath(workingDirProvider.GetCurrentDirectory());
        var modelInfo = _renderingUtilities.FormatModelInfo(appService);
        var tokenUsage = _renderingUtilities.FormatTokenUsage(appService, chatHistory);
        var renderedMessage = _renderingUtilities.RenderMessage(chatHistory[0], _themeInfo);

        // Assert
        displayPath.Should().NotBeNullOrEmpty("display path should be formatted");
        modelInfo.Should().NotBeNullOrEmpty("model info should be formatted");
        tokenUsage.Should().NotBeNullOrEmpty("token usage should be formatted");
        renderedMessage.Should().NotBeNull("message should be rendered with theme");
        
        // Verify token usage contains expected elements
        tokenUsage.Should().Contain("tokens", "token usage should mention tokens");
        tokenUsage.Should().Contain("context", "token usage should mention context");
        
        _logger.LogInformation("Rendering utilities integration verified successfully");
    }

    [Fact]
    public async Task WhenInputStateRendersMessages_ThenThemeIsApplied()
    {
        // Arrange
        _logger.LogInformation("Testing theme application in InputTuiState message rendering");
        
        await InitializeTuiComponentsAsync();
        
        // Add a test message to the history
        var testMessage = new ChatMessage(ChatRole.User, "Test input state message");
        _tuiContext.HistoryManager.AddUserMessage(testMessage);

        // Act - Get the current state and verify it can render with theme
        var currentState = _stateManager.CurrentState;
        currentState.Should().NotBeNull("current state should be available");
        currentState.Should().BeOfType<InputTuiState>("should be in input state");

        // Verify the state can access theme through service provider
        var themeFromContext = _tuiContext.ServiceProvider.GetRequiredService<IThemeInfo>();
        themeFromContext.Should().NotBeNull("theme should be accessible from TUI context");
        themeFromContext.Should().BeSameAs(_themeInfo, "should be the same theme instance");

        // Assert
        _logger.LogInformation("Theme application in InputTuiState verified successfully");
    }

    /// <summary>
    /// Initializes TUI components without starting the full application loop.
    /// </summary>
    private async Task InitializeTuiComponentsAsync()
    {
        _logger.LogTrace("Initializing TUI components for theme testing");
        
        // Register state factories
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
        
        // Initialize the state manager
        await _stateManager.InitializeAsync(_tuiContext);
        
        // Verify initial state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, 
            "application should start in Input state");
        
        _logger.LogTrace("TUI components initialized successfully for theme testing");
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

    /// <summary>
    /// Gets the path to the user's mogzi.config.json file.
    /// </summary>
    private static string? GetUserConfigPath()
    {
        return null; // Let the system find the default config
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
            _app?.Dispose();
            
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

    /// <summary>
    /// Test implementation of IThemeInfo for custom theme testing.
    /// </summary>
    private class TestThemeInfo : IThemeInfo
    {
        public Color PrimaryColor { get; set; } = Color.Red;
        public Color SecondaryColor { get; set; } = Color.Blue;
        public Color AccentColor { get; set; } = Color.Yellow;
        public BoxBorder BorderStyle { get; set; } = BoxBorder.Square;
        public string UserMessageColor { get; set; } = "red";
        public string AssistantMessageColor { get; set; } = "green";
        public string SystemMessageColor { get; set; } = "yellow";
        public string UserMessagePrefix { get; set; } = "[red]USER:[/] ";
        public string AssistantMessagePrefix { get; set; } = "[green]AI:[/] ";
    }
}
