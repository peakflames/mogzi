using Mogzi.PawPrints;
using Xunit.Abstractions;

namespace Mogzi.TUI.Tests;

/// <summary>
/// Test to reproduce and fix the NullReferenceException in FlexColumnMediator.StartAiProcessingWorkflow
/// This test follows TDD approach: write test to catch the bug, see it fail, fix the code, see it pass.
/// </summary>
public class NullReferenceExceptionTest : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FlexColumnTuiApp _app;
    private readonly TestScrollbackTerminal _testTerminal;
    private readonly ITuiContext _tuiContext;
    private readonly ITuiStateManager _stateManager;
    private readonly ITuiMediator _mediator;
    private readonly ILogger<NullReferenceExceptionTest> _logger;
    private readonly ITestOutputHelper? _output;
    private bool _disposed = false;

    public NullReferenceExceptionTest(ITestOutputHelper output)
    {
        _output = output;
        
        // Use the user's mogzi.config.json with the "testing" profile
        var configPath = GetUserConfigPath();
        
        // Build service collection with real dependencies (no mocking except ScrollbackTerminal)
        var services = new ServiceCollection();
        ServiceConfiguration.ConfigureServices(services, configPath, "testing", "readonly");
        
        // Configure logging to show Trace level for debugging
        services.Configure<LoggerFilterOptions>(options =>
        {
            options.MinLevel = LogLevel.Trace;
            options.Rules.Clear();
            options.Rules.Add(new LoggerFilterRule(null, null, LogLevel.Trace, null));
        });
        
        // Replace ScrollbackTerminal with test version to capture static content
        services.AddSingleton<IScrollbackTerminal>(provider => 
        {
            var realConsole = provider.GetRequiredService<IAnsiConsole>();
            return new TestScrollbackTerminal(realConsole);
        });
        
        // Add test-specific logger
        services.AddSingleton<ILogger<NullReferenceExceptionTest>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<NullReferenceExceptionTest>());
        
        _serviceProvider = services.BuildServiceProvider();
        
        // Get required services from DI container
        _app = _serviceProvider.GetRequiredService<FlexColumnTuiApp>();
        _testTerminal = (TestScrollbackTerminal)_serviceProvider.GetRequiredService<IScrollbackTerminal>();
        _tuiContext = _serviceProvider.GetRequiredService<ITuiContext>();
        _stateManager = _serviceProvider.GetRequiredService<ITuiStateManager>();
        _mediator = _serviceProvider.GetRequiredService<ITuiMediator>();
        _logger = _serviceProvider.GetRequiredService<ILogger<NullReferenceExceptionTest>>();
        
        _logger.LogInformation("NullReferenceExceptionTest initialized with real service configuration");
    }

    [Fact]
    public async Task StartAiProcessingWorkflow_WithNullAiOperationCts_ShouldHandleGracefully()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing that StartAiProcessingWorkflow handles null AiOperationCts gracefully");
        _logger.LogInformation("🧪 Testing that StartAiProcessingWorkflow handles null AiOperationCts gracefully");
        
        await InitializeApplicationAsync();
        
        // Add a user message to history so the workflow has something to process
        var userMessage = new ChatMessage(ChatRole.User, "tell me a joke");
        _tuiContext.HistoryManager.AddUserMessage(userMessage);
        
        // Simulate the scenario: AiOperationCts is null when StartAiProcessingWorkflow is called
        // This used to cause a NullReferenceException, but now should be handled gracefully
        _tuiContext.AiOperationCts = null; // This used to be the bug condition
        
        // Act & Assert
        _output?.WriteLine("🔍 Calling StartAiProcessingWorkflow with null AiOperationCts");
        _logger.LogInformation("🔍 Calling StartAiProcessingWorkflow with null AiOperationCts");
        
        // This should NOT throw an exception - the fix should handle null gracefully
        var exception = await Record.ExceptionAsync(async () =>
        {
            // This simulates what ThinkingTuiState.OnEnterAsync does
            await _mediator.StartAiProcessingWorkflow(_tuiContext);
        });
        
        // We expect NO exception - the fix should handle null AiOperationCts gracefully
        exception.Should().BeNull("StartAiProcessingWorkflow should handle null AiOperationCts gracefully after the fix");
        
        // Verify that AiOperationCts was properly initialized
        _tuiContext.AiOperationCts.Should().NotBeNull("AiOperationCts should be initialized by StartAiProcessingWorkflow");
        _tuiContext.AiOperationCts!.Token.Should().NotBeNull("AiOperationCts.Token should be available");
        
        _output?.WriteLine("✅ StartAiProcessingWorkflow handled null AiOperationCts gracefully");
        _logger.LogInformation("✅ StartAiProcessingWorkflow handled null AiOperationCts gracefully");
    }

    [Fact]
    public async Task HandleUserInputAsync_ShouldProperlyInitializeAiOperationCts()
    {
        // Arrange
        _output?.WriteLine("🧪 Testing that HandleUserInputAsync properly initializes AiOperationCts");
        _logger.LogInformation("🧪 Testing that HandleUserInputAsync properly initializes AiOperationCts");
        
        await InitializeApplicationAsync();
        
        // Ensure AiOperationCts starts as null
        _tuiContext.AiOperationCts = null;
        
        // Act
        _output?.WriteLine("🔍 Calling HandleUserInputAsync to initialize AiOperationCts");
        _logger.LogInformation("🔍 Calling HandleUserInputAsync to initialize AiOperationCts");
        
        await _mediator.HandleUserInputAsync("tell me a joke", _tuiContext);
        
        // Assert
        _tuiContext.AiOperationCts.Should().NotBeNull("HandleUserInputAsync should initialize AiOperationCts");
        _tuiContext.AiOperationCts!.Token.Should().NotBeNull("AiOperationCts.Token should be available");
        
        _output?.WriteLine("✅ HandleUserInputAsync properly initialized AiOperationCts");
        _logger.LogInformation("✅ HandleUserInputAsync properly initialized AiOperationCts");
    }

    /// <summary>
    /// Initializes the TUI application components.
    /// </summary>
    private async Task InitializeApplicationAsync()
    {
        _logger.LogTrace("Initializing TUI application for null reference test");
        
        // Register state factories
        _stateManager.RegisterState(ChatState.Input, _serviceProvider.GetRequiredService<InputTuiState>);
        _stateManager.RegisterState(ChatState.Thinking, _serviceProvider.GetRequiredService<ThinkingTuiState>);
        _stateManager.RegisterState(ChatState.ToolExecution, _serviceProvider.GetRequiredService<ToolExecutionTuiState>);
        
        // Initialize the state manager with the TUI context
        await _stateManager.InitializeAsync(_tuiContext);
        
        // Verify we're in the correct initial state
        _stateManager.CurrentStateType.Should().Be(ChatState.Input, 
            "application should start in Input state");
        
        _logger.LogTrace("TUI application initialized successfully");
    }

    /// <summary>
    /// Gets the path to the user's mogzi.config.json file.
    /// </summary>
    private static string? GetUserConfigPath()
    {
        return null; // Let the ChatClient.Create method find the default config path
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        
        try
        {
            _tuiContext?.AiOperationCts?.Dispose();
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
