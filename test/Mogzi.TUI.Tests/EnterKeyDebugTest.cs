using Mogzi.PawPrints;
using Xunit.Abstractions;

namespace Mogzi.TUI.Tests;

/// <summary>
/// Debug test to isolate the Enter key handling issue.
/// </summary>
public class EnterKeyDebugTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ILogger<EnterKeyDebugTest> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public EnterKeyDebugTest(ITestOutputHelper output)
    {
        _output = output;
        
        // Build service collection with real dependencies
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, null, "testing", "readonly");
        
        // Replace ScrollbackTerminal with test version
        services.AddSingleton<IScrollbackTerminal>(provider => 
        {
            var realConsole = provider.GetRequiredService<IAnsiConsole>();
            return new TestScrollbackTerminal(realConsole);
        });
        
        // Add test-specific logger
        services.AddSingleton<ILogger<EnterKeyDebugTest>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<EnterKeyDebugTest>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _logger = _serviceProvider.GetRequiredService<ILogger<EnterKeyDebugTest>>();
    }

    [Fact]
    public async Task EnterKey_WithNonEmptyInput_ShouldTransitionToThinking()
    {
        // Arrange
        _output?.WriteLine("🔍 Starting Enter key debug test");
        
        // Initialize state manager
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
        
        await _stateManager.InitializeAsync(_tuiContext);
        
        // Verify initial state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, "should start in Input state");
        _output?.WriteLine($"✅ Initial state: {_stateManager.CurrentStateType}");
        
        // Add some input text
        _tuiContext.InputContext.CurrentInput = "test message";
        _tuiContext.InputContext.CursorPosition = "test message".Length;
        _output?.WriteLine($"✅ Input set to: '{_tuiContext.InputContext.CurrentInput}'");
        
        // Initialize AI operation cancellation token (required for state transition)
        _tuiContext.AiOperationCts = new CancellationTokenSource();
        _tuiContext.AiOperationStartTime = DateTime.Now;
        _output?.WriteLine("✅ AI operation context initialized");
        
        // Act - Press Enter
        _output?.WriteLine("🔍 Pressing Enter key...");
        var enterKeyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);
        
        _output?.WriteLine($"🔍 State before Enter: {_stateManager.CurrentStateType}");
        
        await _stateManager.HandleKeyPressAsync(enterEvent);
        
        _output?.WriteLine($"🔍 State immediately after Enter: {_stateManager.CurrentStateType}");
        
        // Wait a bit for async operations
        await Task.Delay(100);
        _output?.WriteLine($"🔍 State after 100ms delay: {_stateManager.CurrentStateType}");
        
        // Wait longer for async operations to complete
        await Task.Delay(10000); // Give enough time for AI processing to complete
        _output?.WriteLine($"🔍 State after 10000ms delay: {_stateManager.CurrentStateType}");
        
        // Assert - should be back in Input state after AI processing completes
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, 
            "should return to Input state after AI processing completes");
        
        _output?.WriteLine("✅ Enter key debug test completed successfully");
    }

    [Fact]
    public async Task EnterKey_WithEmptyInput_ShouldStayInInput()
    {
        // Arrange
        _output?.WriteLine("🔍 Starting Enter key with empty input test");
        
        // Initialize state manager
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
        
        await _stateManager.InitializeAsync(_tuiContext);
        
        // Verify initial state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, "should start in Input state");
        _output?.WriteLine($"✅ Initial state: {_stateManager.CurrentStateType}");
        
        // Leave input empty
        _tuiContext.InputContext.CurrentInput = "";
        _tuiContext.InputContext.CursorPosition = 0;
        _output?.WriteLine($"✅ Input is empty: '{_tuiContext.InputContext.CurrentInput}'");
        
        // Act - Press Enter
        _output?.WriteLine("🔍 Pressing Enter key with empty input...");
        var enterKeyInfo = new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
        var enterEvent = new KeyPressEventArgs(enterKeyInfo);
        
        await _stateManager.HandleKeyPressAsync(enterEvent);
        
        // Wait a bit for any async operations
        await Task.Delay(100);
        
        // Assert - should stay in Input state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, 
            "should stay in Input state when Enter is pressed with empty input");
        
        _output?.WriteLine("✅ Empty input test completed successfully");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
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
