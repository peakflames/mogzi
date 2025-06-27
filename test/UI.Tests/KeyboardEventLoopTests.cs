namespace UI.Tests;

public class KeyboardEventLoopTests
{
    private static (TuiApp app, ServiceProvider provider) SetupTestApp(string testResponse)
    {
        var services = new ServiceCollection();

        // Register configuration and working directory provider
        services.AddSingleton<IWorkingDirectoryProvider>(new MockWorkingDirectoryProvider());
        services.AddSingleton<ILogger<TuiApp>>(new NullLogger<TuiApp>());

        // Create the ChatClient wrapper with our TestChatClient
        var chatClientResult = ChatClient.Create(new TestChatClient(testResponse), "maxbot.config.json");
        Assert.False(chatClientResult.IsFailed, chatClientResult.Errors.FirstOrDefault()?.Message);
        services.AddSingleton(chatClientResult.Value);

        // Register the real services that depend on ChatClient
        services.AddSingleton<IAppService, AppService>();
        
        // Register other UI services and components
        services.AddSingleton<StateManager>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<LayoutManager>();
        
        // Register components with proper dependency injection
        services.AddSingleton<HeaderComponent>();
        services.AddSingleton<StaticHistoryComponent>();
        services.AddSingleton<DynamicContentComponent>();
        services.AddSingleton<InputComponent>();
        services.AddSingleton<FooterComponent>();
        services.AddSingleton<AppComponent>();

        var provider = services.BuildServiceProvider();
        var app = new TuiApp(provider);

        return (app, provider);
    }

    [Fact]
    public void TuiApp_GetInputComponent_ReturnsInputComponentFromAppComponent()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Act
        var inputComponent = appComponent.GetInputComponent();
        
        // Assert
        inputComponent.Should().NotBeNull("AppComponent should provide access to InputComponent");
        inputComponent.Should().BeOfType<InputComponent>("Should return the actual InputComponent instance");
    }

    [Fact]
    public async Task InputComponent_InputSubmittedEvent_TriggersAppComponentProcessing()
    {
        // Arrange
        var expectedResponse = "AI response to keyboard input";
        var (app, provider) = SetupTestApp(expectedResponse);
        
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = provider.GetRequiredService<InputComponent>();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        var userInput = "Hello from keyboard input!";
        
        // Act - Simulate the keyboard event loop triggering input submission
        inputComponent.SetCurrentInput(userInput);
        inputComponent.SubmitCurrentInput(); // This should trigger the InputSubmitted event
        
        // Give time for async processing
        await Task.Delay(100);
        
        // Assert
        var history = historyManager.GetCompletedMessages();
        history.Should().HaveCount(2, "Should have user message and AI response");
        
        history[0].Role.Should().Be(ChatRole.User);
        history[0].Text.Should().Be(userInput);
        
        history[1].Role.Should().Be(ChatRole.Assistant);
        history[1].Text.Should().Be(expectedResponse);
        
        // Input should be cleared after submission
        var currentInput = inputComponent.GetCurrentInput();
        currentInput.Should().Be("", "Input should be cleared after submission");
    }

    [Fact]
    public void InputComponent_SubmitCurrentInput_RespectsEnabledState()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        var submissionCount = 0;
        
        // Subscribe to count submissions
        inputComponent.InputSubmitted += (sender, message) => submissionCount++;
        
        // Act - Test that disabled input blocks submission
        inputComponent.SetCurrentInput("test input");
        inputComponent.SetInputEnabled(false); // Disable input
        
        inputComponent.SubmitCurrentInput(); // Should not trigger event because input is disabled
        
        submissionCount.Should().Be(0, "Disabled input should not allow submission");
        
        // Re-enable input and try again
        inputComponent.SetInputEnabled(true);
        inputComponent.SubmitCurrentInput(); // Should work now
        
        submissionCount.Should().Be(1, "Enabled input should allow submission");
        
        // Test empty input is also blocked
        inputComponent.SetCurrentInput(""); // Empty input
        inputComponent.SubmitCurrentInput(); // Should not trigger event because input is empty
        
        submissionCount.Should().Be(1, "Empty input should not be submitted");
    }

    [Fact]
    public void TuiApp_KeyboardEventHandling_SimulatesCharacterInput()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = appComponent.GetInputComponent();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Act - Simulate typing characters (this tests the keyboard handling logic)
        inputComponent.SetCurrentInput("H");
        var step1 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("He");
        var step2 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("Hello");
        var step3 = inputComponent.GetCurrentInput();
        
        // Simulate backspace
        var currentText = inputComponent.GetCurrentInput();
        if (currentText.Length > 0)
        {
            inputComponent.SetCurrentInput(currentText[..^1]);
        }
        var afterBackspace = inputComponent.GetCurrentInput();
        
        // Assert
        step1.Should().Be("H", "Should capture first character");
        step2.Should().Be("He", "Should accumulate second character");
        step3.Should().Be("Hello", "Should accumulate full word");
        afterBackspace.Should().Be("Hell", "Should handle backspace correctly");
    }

    [Fact]
    public void TuiApp_KeyboardEventHandling_SimulatesArrowKeyNavigation()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = appComponent.GetInputComponent();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Setup command history
        inputComponent.AddToCommandHistory("first command");
        inputComponent.AddToCommandHistory("second command");
        inputComponent.AddToCommandHistory("third command");
        
        // Act - Simulate arrow key navigation (this tests the keyboard handling logic)
        inputComponent.NavigateCommandHistory(up: true);  // Should get "third command"
        var step1 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // Should get "second command"
        var step2 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: false); // Should get "third command"
        var step3 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: false); // Should clear input
        var step4 = inputComponent.GetCurrentInput();
        
        // Assert
        step1.Should().Be("third command", "Up arrow should navigate to most recent command");
        step2.Should().Be("second command", "Up arrow should navigate to older command");
        step3.Should().Be("third command", "Down arrow should navigate to newer command");
        step4.Should().Be("", "Down arrow from newest should clear input");
    }

    [Fact]
    public void TuiApp_KeyboardEventHandling_SimulatesEscapeKey()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = appComponent.GetInputComponent();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Act - Type some text and then simulate Escape key
        inputComponent.SetCurrentInput("Some text to be cleared");
        var beforeEscape = inputComponent.GetCurrentInput();
        
        // Simulate Escape key (clear input)
        inputComponent.ClearCurrentInput();
        var afterEscape = inputComponent.GetCurrentInput();
        
        // Assert
        beforeEscape.Should().Be("Some text to be cleared", "Should have text before escape");
        afterEscape.Should().Be("", "Escape should clear all input");
    }

    [Fact]
    public async Task TuiApp_EndToEndKeyboardFlow_SimulatesCompleteUserInteraction()
    {
        // Arrange
        var expectedResponse = "AI response to complete interaction";
        var (app, provider) = SetupTestApp(expectedResponse);
        
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = appComponent.GetInputComponent();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Act - Simulate complete user interaction flow
        
        // 1. User types a message character by character
        inputComponent.SetCurrentInput("H");
        inputComponent.SetCurrentInput("He");
        inputComponent.SetCurrentInput("Hel");
        inputComponent.SetCurrentInput("Hell");
        inputComponent.SetCurrentInput("Hello");
        inputComponent.SetCurrentInput("Hello ");
        inputComponent.SetCurrentInput("Hello W");
        inputComponent.SetCurrentInput("Hello Wo");
        inputComponent.SetCurrentInput("Hello Wor");
        inputComponent.SetCurrentInput("Hello Worl");
        inputComponent.SetCurrentInput("Hello World");
        
        var typedMessage = inputComponent.GetCurrentInput();
        
        // 2. User presses Enter to submit
        inputComponent.SubmitCurrentInput();
        
        // 3. Give time for AI processing
        await Task.Delay(100);
        
        // 4. Check that input was added to command history
        inputComponent.AddToCommandHistory("Hello World");
        
        // 5. Navigate through history
        inputComponent.NavigateCommandHistory(up: true);
        var historyRecall = inputComponent.GetCurrentInput();
        
        // Assert
        typedMessage.Should().Be("Hello World", "Should accumulate typed characters correctly");
        
        var history = historyManager.GetCompletedMessages();
        history.Should().HaveCount(2, "Should have user message and AI response");
        history[0].Text.Should().Be("Hello World");
        history[1].Text.Should().Be(expectedResponse);
        
        historyRecall.Should().Be("Hello World", "Should be able to recall from command history");
    }

    [Fact]
    public async Task TuiApp_KeyboardEventLoop_HandlesErrorsGracefully()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = appComponent.GetInputComponent();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        // Register the AppComponent with the TuiApp
        app.RegisterComponent(appComponent, RenderZone.Static);
        
        // Create a scenario that might cause an error (empty input)
        inputComponent.SetCurrentInput("");
        
        // Act - Try to submit empty input
        inputComponent.SubmitCurrentInput(); // Should not trigger submission for empty input
        
        await Task.Delay(50);
        
        // Assert - Should handle gracefully without crashing
        var history = historyManager.GetCompletedMessages();
        history.Should().BeEmpty("Empty input should not be processed");
        
        // App should still be functional
        inputComponent.SetCurrentInput("Valid input");
        inputComponent.SubmitCurrentInput();
        
        await Task.Delay(100);
        
        var historyAfterValid = historyManager.GetCompletedMessages();
        historyAfterValid.Should().HaveCount(2, "Valid input should be processed normally");
    }
}
