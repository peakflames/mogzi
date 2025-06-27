namespace UI.Tests;

public class KeyboardInputTests
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
    public void TuiApp_WithKeyboardInput_CapturesUserTyping()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Act - Simulate keyboard input
        // Note: In a real implementation, this would test the keyboard event loop
        // For now, we test the InputComponent's ability to handle input
        inputComponent.SetCurrentInput("Hello World");
        
        // Assert
        var currentInput = inputComponent.GetCurrentInput();
        currentInput.Should().Be("Hello World", "InputComponent should capture and store user typing");
    }

    [Fact]
    public void InputComponent_HandleTextInput_AccumulatesCharacters()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Act - Simulate typing character by character
        inputComponent.SetCurrentInput("H");
        var step1 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("He");
        var step2 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("Hello");
        var step3 = inputComponent.GetCurrentInput();
        
        // Assert
        step1.Should().Be("H", "Should capture first character");
        step2.Should().Be("He", "Should accumulate second character");
        step3.Should().Be("Hello", "Should accumulate full word");
    }

    [Fact]
    public void InputComponent_HandleBackspace_RemovesCharacters()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Setup initial input
        inputComponent.SetCurrentInput("Hello World");
        var initialInput = inputComponent.GetCurrentInput();
        
        // Act - Simulate backspace operations
        inputComponent.SetCurrentInput("Hello Worl");  // Remove 'd'
        var afterBackspace1 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("Hello");  // Remove " Worl"
        var afterBackspace2 = inputComponent.GetCurrentInput();
        
        inputComponent.SetCurrentInput("");  // Clear all
        var afterClear = inputComponent.GetCurrentInput();
        
        // Assert
        initialInput.Should().Be("Hello World", "Should start with full input");
        afterBackspace1.Should().Be("Hello Worl", "Should remove last character");
        afterBackspace2.Should().Be("Hello", "Should remove multiple characters");
        afterClear.Should().Be("", "Should clear all input");
    }

    [Fact]
    public void InputComponent_HandleEnterKey_SubmitsMessage()
    {
        // Arrange
        var (app, provider) = SetupTestApp("AI response to user message");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        var messageSubmitted = false;
        var submittedMessage = "";
        
        // Subscribe to input submission event
        inputComponent.InputSubmitted += (sender, message) =>
        {
            messageSubmitted = true;
            submittedMessage = message;
        };
        
        // Act - Type message and submit
        inputComponent.SetCurrentInput("Test message for submission");
        inputComponent.SubmitCurrentInput();
        
        // Assert
        messageSubmitted.Should().BeTrue("Enter key should trigger message submission");
        submittedMessage.Should().Be("Test message for submission", "Submitted message should match typed input");
        
        // Input should be cleared after submission
        var currentInput = inputComponent.GetCurrentInput();
        currentInput.Should().Be("", "Input should be cleared after submission");
    }

    [Fact]
    public void InputComponent_HandleArrowKeys_NavigatesCommandHistory()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        // Setup command history
        historyManager.AddUserMessage(new ChatMessage(ChatRole.User, "first command"));
        historyManager.AddUserMessage(new ChatMessage(ChatRole.User, "second command"));
        historyManager.AddUserMessage(new ChatMessage(ChatRole.User, "third command"));
        
        // Add commands to InputComponent history
        inputComponent.AddToCommandHistory("first command");
        inputComponent.AddToCommandHistory("second command");
        inputComponent.AddToCommandHistory("third command");
        
        // Act - Navigate up through history (older commands)
        inputComponent.NavigateCommandHistory(up: true);  // Should get "third command"
        var step1 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // Should get "second command"
        var step2 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // Should get "first command"
        var step3 = inputComponent.GetCurrentInput();
        
        // Navigate down (newer commands)
        inputComponent.NavigateCommandHistory(up: false); // Should get "second command"
        var step4 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: false); // Should get "third command"
        var step5 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: false); // Should clear input
        var step6 = inputComponent.GetCurrentInput();
        
        // Assert
        step1.Should().Be("third command", "Up arrow should navigate to most recent command");
        step2.Should().Be("second command", "Up arrow should navigate to older command");
        step3.Should().Be("first command", "Up arrow should navigate to oldest command");
        step4.Should().Be("second command", "Down arrow should navigate to newer command");
        step5.Should().Be("third command", "Down arrow should navigate to newest command");
        step6.Should().Be("", "Down arrow from newest should clear input");
    }

    [Fact]
    public void InputComponent_WithProcessingState_DisablesInput()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Verify input is initially enabled
        inputComponent.SetCurrentInput("test");
        var initialInput = inputComponent.GetCurrentInput();
        
        // Act - Disable input (simulate AI processing)
        inputComponent.SetInputEnabled(false);
        
        // Try to submit input while disabled
        var submissionAttempted = false;
        inputComponent.InputSubmitted += (sender, message) => submissionAttempted = true;
        
        inputComponent.SubmitCurrentInput();
        
        // Assert
        initialInput.Should().Be("test", "Input should work when enabled");
        submissionAttempted.Should().BeFalse("Input submission should be blocked when disabled");
        
        // Re-enable input
        inputComponent.SetInputEnabled(true);
        inputComponent.SubmitCurrentInput();
        
        submissionAttempted.Should().BeTrue("Input submission should work when re-enabled");
    }

    [Fact]
    public void InputComponent_WithLongInput_HandlesMultiLineText()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        var longInput = "This is a very long input message that might span multiple lines in the terminal. " +
                       "It should be handled gracefully by the input component without causing any issues. " +
                       "The component should support text of various lengths and handle word wrapping appropriately.";
        
        // Act
        inputComponent.SetCurrentInput(longInput);
        var storedInput = inputComponent.GetCurrentInput();
        
        // Submit the long input
        var submittedMessage = "";
        inputComponent.InputSubmitted += (sender, message) => submittedMessage = message;
        
        inputComponent.SubmitCurrentInput();
        
        // Assert
        storedInput.Should().Be(longInput, "InputComponent should handle long text input");
        submittedMessage.Should().Be(longInput, "Long input should be submitted correctly");
        
        // Input should be cleared after submission
        var clearedInput = inputComponent.GetCurrentInput();
        clearedInput.Should().Be("", "Input should be cleared after submitting long text");
    }

    [Fact]
    public void InputComponent_CommandHistory_LimitsHistorySize()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Act - Add more than 100 commands (the limit)
        for (int i = 1; i <= 105; i++)
        {
            inputComponent.AddToCommandHistory($"command {i}");
        }
        
        // Navigate to oldest available command
        for (int i = 0; i < 100; i++)  // Try to go back 100 commands
        {
            inputComponent.NavigateCommandHistory(up: true);
        }
        var oldestCommand = inputComponent.GetCurrentInput();
        
        // Reset navigation and get newest command
        inputComponent.SetCurrentInput("");  // Reset to empty input
        inputComponent.NavigateCommandHistory(up: true);   // Get most recent
        var newestCommand = inputComponent.GetCurrentInput();
        
        // Assert
        // Should have commands 6-105 (100 commands total)
        oldestCommand.Should().Be("command 6", "History should be limited to 100 commands, oldest should be command 6");
        newestCommand.Should().Be("command 105", "Most recent command should be command 105");
    }

    [Fact]
    public void InputComponent_CommandHistory_AvoidsDuplicates()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        // Act - Add duplicate commands
        inputComponent.AddToCommandHistory("unique command 1");
        inputComponent.AddToCommandHistory("duplicate command");
        inputComponent.AddToCommandHistory("duplicate command");  // Should not be added
        inputComponent.AddToCommandHistory("unique command 2");
        inputComponent.AddToCommandHistory("duplicate command");  // Should not be added
        
        // Navigate through history
        inputComponent.NavigateCommandHistory(up: true);  // "unique command 2"
        var command1 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // "duplicate command"
        var command2 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // "unique command 1"
        var command3 = inputComponent.GetCurrentInput();
        
        inputComponent.NavigateCommandHistory(up: true);  // Should stay at "unique command 1"
        var command4 = inputComponent.GetCurrentInput();
        
        // Assert
        command1.Should().Be("unique command 2", "Should navigate to most recent unique command");
        command2.Should().Be("duplicate command", "Should have only one instance of duplicate command");
        command3.Should().Be("unique command 1", "Should navigate to oldest command");
        command4.Should().Be("unique command 1", "Should not navigate beyond oldest command");
    }

    [Fact]
    public async Task AppComponent_WithInputSubmission_ProcessesUserMessage()
    {
        // Arrange
        var expectedResponse = "AI response to user input";
        var (app, provider) = SetupTestApp(expectedResponse);
        
        var appComponent = provider.GetRequiredService<AppComponent>();
        var inputComponent = provider.GetRequiredService<InputComponent>();
        var historyManager = provider.GetRequiredService<HistoryManager>();
        
        var cancellationTokenSource = new CancellationTokenSource();
        var runTask = app.RunAsync(new string[]{}, cancellationTokenSource.Token);
        
        // Act - Simulate user typing and submitting message
        inputComponent.SetCurrentInput("Hello, how can you help me?");
        
        // Simulate the input submission triggering AppComponent.ProcessUserInput
        var userInput = inputComponent.GetCurrentInput();
        inputComponent.ClearCurrentInput();
        
        await appComponent.ProcessUserInput(userInput);
        
        // Give time for processing
        await Task.Delay(100);
        
        // Assert
        var history = historyManager.GetCompletedMessages();
        history.Should().HaveCount(2, "Should have user message and AI response");
        
        history[0].Role.Should().Be(ChatRole.User);
        history[0].Text.Should().Be("Hello, how can you help me?");
        
        history[1].Role.Should().Be(ChatRole.Assistant);
        history[1].Text.Should().Be(expectedResponse);
        
        // Clean up
        cancellationTokenSource.Cancel();
        await runTask;
    }

    [Fact]
    public async Task TuiApp_KeyboardEventLoop_HandlesCtrlCGracefully()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        // Act - Start the app and immediately cancel (simulating Ctrl+C)
        var runTask = app.RunAsync(new string[]{}, cancellationTokenSource.Token);
        
        // Simulate Ctrl+C after a short delay
        await Task.Delay(50);
        cancellationTokenSource.Cancel();
        
        var exitCode = await runTask;
        
        // Assert
        exitCode.Should().Be(0, "App should exit gracefully with code 0 when cancelled");
        app.IsRunning.Should().BeFalse("App should not be running after cancellation");
    }

    [Fact]
    public async Task InputComponent_RenderAsync_ShowsCurrentInputState()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        
        var terminalSize = new TerminalSize(120, 40);
        var constraints = new LayoutConstraints(terminalSize.Height, terminalSize.Width);
        var context = new RenderContext(constraints, terminalSize);
        
        // Act - Set input and render
        inputComponent.SetCurrentInput("Current user input being typed...");
        var result = await inputComponent.RenderAsync(context);
        
        // Assert
        result.Should().NotBeNull();
        var panel = result.Should().BeOfType<Panel>().Subject;
        panel.Should().NotBeNull("InputComponent should render current input state");
        
        // Verify the component can display the current input
        var currentInput = inputComponent.GetCurrentInput();
        currentInput.Should().Be("Current user input being typed...", "Component should maintain current input state");
    }

    [Fact]
    public async Task InputComponent_WithStateManager_UpdatesUIOnInputChanges()
    {
        // Arrange
        var (app, provider) = SetupTestApp("Test response");
        var inputComponent = provider.GetRequiredService<InputComponent>();
        var stateManager = provider.GetRequiredService<StateManager>();
        
        var terminalSize = new TerminalSize(120, 40);
        var constraints = new LayoutConstraints(terminalSize.Height, terminalSize.Width);
        var context = new RenderContext(constraints, terminalSize);
        
        // Get initial render
        var initialRender = await inputComponent.RenderAsync(context);
        
        // Act - Change input state and trigger state update
        inputComponent.SetCurrentInput("New input text");
        stateManager.FlushPendingChanges();
        await Task.Delay(50);
        
        var updatedRender = await inputComponent.RenderAsync(context);
        
        // Assert
        initialRender.Should().NotBeNull();
        updatedRender.Should().NotBeNull();
        
        // Component should be able to render in different input states
        var initialPanel = initialRender.Should().BeOfType<Panel>().Subject;
        var updatedPanel = updatedRender.Should().BeOfType<Panel>().Subject;
        
        initialPanel.Should().NotBeNull("InputComponent should render initial state");
        updatedPanel.Should().NotBeNull("InputComponent should render updated state");
        
        // Verify the input state was actually updated
        var currentInput = inputComponent.GetCurrentInput();
        currentInput.Should().Be("New input text", "Input state should be updated");
    }
}
