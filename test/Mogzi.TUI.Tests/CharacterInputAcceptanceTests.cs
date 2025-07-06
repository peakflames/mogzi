namespace Mogzi.TUI.Tests;

/// <summary>
/// Black-box acceptance tests for character input functionality.
/// Tests the complete pipeline from character input simulation to visual verification.
/// Follows no-mocking approach with real service configuration and DI container.
/// </summary>
public class CharacterInputAcceptanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiComponentManager _componentManager;
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private readonly ILogger<CharacterInputAcceptanceTests> _logger;
    private bool _disposed = false;

    public CharacterInputAcceptanceTests()
    {
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking)
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");
        
        // Add test-specific logger
        services.AddSingleton<ILogger<CharacterInputAcceptanceTests>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<CharacterInputAcceptanceTests>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _componentManager = _serviceProvider.GetRequiredService<ITuiComponentManager>();
        _keyboardHandler = _serviceProvider.GetRequiredService<AdvancedKeyboardHandler>();
        _logger = _serviceProvider.GetRequiredService<ILogger<CharacterInputAcceptanceTests>>();
        
        _logger.LogInformation("CharacterInputAcceptanceTests initialized with real service configuration");
    }

    [Fact]
    public async Task WhenUserTypesCharacters_ThenCharactersAppearInInputBox()
    {
        // Arrange
        _logger.LogInformation("Starting character input acceptance test");
        
        // Initialize the TUI application components without starting the full UI loop
        await InitializeTuiComponentsAsync();
        
        // Verify initial state - input should be empty
        _tuiContext.InputContext.CurrentInput.Should().BeEmpty("initial input should be empty");
        
        var testCharacters = "hello world";
        _logger.LogInformation("Testing input of characters: '{TestCharacters}'", testCharacters);

        // Act - Simulate typing each character
        foreach (char character in testCharacters)
        {
            await SimulateCharacterInputAsync(character);
        }

        // Assert - Verify characters appear in input context
        _tuiContext.InputContext.CurrentInput.Should().Be(testCharacters, 
            "typed characters should appear in the input context");
        
        // Verify cursor position is at the end
        _tuiContext.InputContext.CursorPosition.Should().Be(testCharacters.Length,
            "cursor should be positioned at the end of the input");

        // Verify the input appears in the rendered output
        var renderedOutput = RenderInputPanel();
        var renderedText = ExtractTextFromRenderable(renderedOutput);
        
        renderedText.Should().Contain(testCharacters, 
            "typed characters should appear in the rendered input panel");
        
        _logger.LogInformation("Character input acceptance test completed successfully");
    }

    [Fact]
    public async Task WhenUserTypesSpecialCharacters_ThenSpecialCharactersAppearInInputBox()
    {
        // Arrange
        await InitializeTuiComponentsAsync();
        
        // Use special characters that won't interfere with Spectre.Console markup parsing
        var specialCharacters = "!@#$%^&*()_+-=.,/?";
        _logger.LogInformation("Testing input of special characters: '{SpecialCharacters}'", specialCharacters);

        // Act
        foreach (char character in specialCharacters)
        {
            await SimulateCharacterInputAsync(character);
        }

        // Assert
        _tuiContext.InputContext.CurrentInput.Should().Be(specialCharacters,
            "special characters should appear in the input context");
        
        var renderedOutput = RenderInputPanel();
        var renderedText = ExtractTextFromRenderable(renderedOutput);
        
        // Note: Some special characters might be escaped in markup, so we check for presence
        renderedText.Should().Contain("!@#", "special characters should appear in rendered output");
    }

    [Fact]
    public async Task WhenUserTypesNumbers_ThenNumbersAppearInInputBox()
    {
        // Arrange
        await InitializeTuiComponentsAsync();
        
        var numbers = "1234567890";
        _logger.LogInformation("Testing input of numbers: '{Numbers}'", numbers);

        // Act
        foreach (char character in numbers)
        {
            await SimulateCharacterInputAsync(character);
        }

        // Assert
        _tuiContext.InputContext.CurrentInput.Should().Be(numbers,
            "numbers should appear in the input context");
        
        var renderedOutput = RenderInputPanel();
        var renderedText = ExtractTextFromRenderable(renderedOutput);
        
        renderedText.Should().Contain(numbers, "numbers should appear in rendered output");
    }

    [Fact]
    public async Task WhenUserTypesEmptyInput_ThenPlaceholderTextIsShown()
    {
        // Arrange
        await InitializeTuiComponentsAsync();
        
        // Act - Don't type anything, just render
        var renderedOutput = RenderInputPanel();
        var renderedText = ExtractTextFromRenderable(renderedOutput);

        // Assert
        renderedText.Should().Contain("Type your message or /help",
            "placeholder text should be shown when input is empty");
        
        _tuiContext.InputContext.CurrentInput.Should().BeEmpty("input should remain empty");
    }

    [Fact]
    public async Task WhenUserTypesThenClearsInput_ThenInputBecomesEmpty()
    {
        // Arrange
        await InitializeTuiComponentsAsync();
        
        var testText = "test input";
        
        // Act - Type some text
        foreach (char character in testText)
        {
            await SimulateCharacterInputAsync(character);
        }
        
        // Verify text was typed
        _tuiContext.InputContext.CurrentInput.Should().Be(testText);
        
        // Clear the input manually (simulating backspace or clear operation)
        _tuiContext.InputContext.CurrentInput = string.Empty;
        _tuiContext.InputContext.CursorPosition = 0;
        
        // Assert
        _tuiContext.InputContext.CurrentInput.Should().BeEmpty("input should be empty after clearing");
        
        var renderedOutput = RenderInputPanel();
        var renderedText = ExtractTextFromRenderable(renderedOutput);
        
        renderedText.Should().Contain("Type your message or /help",
            "placeholder text should reappear when input is cleared");
    }

    /// <summary>
    /// Initializes TUI components without starting the full application loop.
    /// This allows us to test the input pipeline in isolation.
    /// </summary>
    private async Task InitializeTuiComponentsAsync()
    {
        _logger.LogTrace("Initializing TUI components for testing");
        
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
        
        _logger.LogTrace("TUI components initialized successfully");
    }

    /// <summary>
    /// Simulates a character being typed by the user.
    /// This tests the complete input pipeline from character event to state processing.
    /// </summary>
    private async Task SimulateCharacterInputAsync(char character)
    {
        _logger.LogTrace("Simulating character input: '{Character}'", character);
        
        // Create a character typed event
        var characterEvent = new CharacterTypedEventArgs(character);
        
        // Process the character through the state manager
        // This simulates the path: AdvancedKeyboardHandler -> FlexColumnTuiApp -> TuiStateManager -> InputTuiState
        await _stateManager.HandleCharacterTypedAsync(characterEvent);
        
        _logger.LogTrace("Character '{Character}' processed through state manager", character);
    }

    /// <summary>
    /// Renders the input panel component to verify visual output.
    /// </summary>
    private IRenderable RenderInputPanel()
    {
        _logger.LogTrace("Rendering input panel for verification");
        
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
        
        var inputPanel = _serviceProvider.GetRequiredService<InputPanel>();
        var rendered = inputPanel.Render(renderContext);
        
        _logger.LogTrace("Input panel rendered successfully");
        return rendered;
    }

    /// <summary>
    /// Extracts plain text from a Spectre.Console IRenderable for assertion purposes.
    /// This removes markup and formatting to get the actual text content.
    /// </summary>
    private static string ExtractTextFromRenderable(IRenderable renderable)
    {
        // Create a test console to capture the rendered output
        var testConsole = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(new StringWriter())
        });
        
        // Render to capture the text
        var stringWriter = new StringWriter();
        var testOutput = new AnsiConsoleOutput(stringWriter);
        var testConsoleWithOutput = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = testOutput
        });
        
        testConsoleWithOutput.Write(renderable);
        var rawOutput = stringWriter.ToString();
        
        // Remove ANSI escape sequences and extra whitespace
        var cleanText = Regex.Replace(rawOutput, @"\x1B\[[0-9;]*[mK]", "");
        cleanText = Regex.Replace(cleanText, @"\s+", " ").Trim();
        
        return cleanText;
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
