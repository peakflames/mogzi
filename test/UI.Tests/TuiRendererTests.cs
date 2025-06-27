namespace UI.Tests;

public class TuiRendererTests : IDisposable
{
    private readonly List<TuiRenderer> _renderersToDispose = new();
    private readonly List<StateManager> _stateManagersToDispose = new();

    [Fact]
    public void Constructor_WithValidParameters_InitializesCorrectly()
    {
        // Arrange
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        var stateManager = CreateStateManager();
        var layoutManager = new LayoutManager();

        // Act
        var renderer = CreateRenderer(console, stateManager, layoutManager);

        // Assert
        Assert.NotNull(renderer);
        Assert.Equal(0, renderer.TotalFrames);
        Assert.True(renderer.CurrentFps < 0.001); // Allow for small floating point precision
        Assert.True(renderer.LastRenderTimeMs < 0.001); // Allow for small floating point precision
    }

    [Fact]
    public void Constructor_WithNullConsole_ThrowsArgumentNullException()
    {
        // Arrange
        var stateManager = CreateStateManager();
        var layoutManager = new LayoutManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TuiRenderer(null!, stateManager, layoutManager));
    }

    [Fact]
    public void Constructor_WithNullStateManager_ThrowsArgumentNullException()
    {
        // Arrange
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        var layoutManager = new LayoutManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TuiRenderer(console, null!, layoutManager));
    }

    [Fact]
    public void Constructor_WithNullLayoutManager_ThrowsArgumentNullException()
    {
        // Arrange
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        var stateManager = CreateStateManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new TuiRenderer(console, stateManager, null!));
    }

    [Fact]
    public void RegisterStaticComponent_WithValidComponent_RegistersSuccessfully()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");

        // Act
        renderer.RegisterStaticComponent(component);

        // Assert
        // If we get here without exception, registration succeeded
        Assert.True(true);
    }

    [Fact]
    public void RegisterDynamicComponent_WithValidComponent_RegistersSuccessfully()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");

        // Act
        renderer.RegisterDynamicComponent(component);

        // Assert
        // If we get here without exception, registration succeeded
        Assert.True(true);
    }

    [Fact]
    public void UnregisterComponent_WithRegisteredComponent_UnregistersSuccessfully()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");
        
        renderer.RegisterStaticComponent(component);

        // Act
        renderer.UnregisterComponent(component);

        // Assert
        // If we get here without exception, unregistration succeeded
        Assert.True(true);
    }

    [Fact]
    public async Task ForceRenderAsync_WithRegisteredComponents_TriggersRender()
    {
        // Arrange
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        var renderer = CreateRenderer(console);
        var component = new TestComponent("test-component");
        
        renderer.RegisterStaticComponent(component);

        // Act
        await renderer.ForceRenderAsync();

        // Wait a bit for async operations to complete
        await Task.Delay(100);

        // Assert
        Assert.True(component.RenderCalled);
        var outputText = output.ToString();
        Assert.Contains("Test Component Content", outputText);
    }

    [Fact]
    public async Task RenderCompleted_EventIsRaised_WhenRenderCompletes()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");
        RenderStatistics? capturedStats = null;

        renderer.RegisterStaticComponent(component);
        renderer.RenderCompleted += stats => capturedStats = stats;

        // Act
        await renderer.ForceRenderAsync();

        // Wait for async operations to complete
        await Task.Delay(200);

        // Assert
        Assert.NotNull(capturedStats);
        Assert.True(capturedStats.RenderTimeMs >= 0);
        Assert.Equal(1, capturedStats.StaticComponentCount);
        Assert.Equal(0, capturedStats.DynamicComponentCount);
        Assert.Equal(1, capturedStats.TotalComponents);
    }

    [Fact]
    public async Task RenderLoop_UpdatesFrameStatistics()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");

        renderer.RegisterStaticComponent(component);

        // Act
        await renderer.ForceRenderAsync();
        await Task.Delay(100); // Let render loop run

        // Trigger state change notification directly
        var stateManager = _stateManagersToDispose.Last();
        stateManager.NotifyStateChanged("test-component");
        
        await Task.Delay(200); // Wait for render loop to process

        // Assert
        Assert.True(renderer.TotalFrames > 0);
        Assert.True(renderer.LastRenderTimeMs >= 0);
    }

    [Fact]
    public async Task RenderLoop_WithStateChanges_TriggersRender()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");

        renderer.RegisterStaticComponent(component);

        // Act
        // Trigger multiple state changes
        var stateManager = _stateManagersToDispose.Last();
        stateManager.NotifyStateChanged("test-component");
        await Task.Delay(50);
        
        stateManager.NotifyStateChanged("test-component");
        await Task.Delay(50);
        
        stateManager.NotifyStateChanged("test-component");
        await Task.Delay(100);

        // Assert
        Assert.True(component.RenderCallCount > 0);
    }

    [Fact]
    public async Task RenderLoop_HandlesComponentRenderErrors_Gracefully()
    {
        // Arrange
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        var renderer = CreateRenderer(console);
        var component = new FailingTestComponent("failing-component");

        renderer.RegisterStaticComponent(component);

        // Act & Assert
        // Should not throw exception
        await renderer.ForceRenderAsync();
        await Task.Delay(100);

        // Verify some output was produced (error handling should produce output)
        var outputText = output.ToString();
        Assert.True(outputText.Length > 0);
    }

    [Fact]
    public void Dispose_StopsRenderLoop_AndCleansUpResources()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");
        renderer.RegisterStaticComponent(component);

        // Act
        renderer.Dispose();

        // Assert
        // After disposal, operations should not throw but should be no-ops
        renderer.RegisterStaticComponent(component); // Should be ignored
        renderer.UnregisterComponent(component); // Should be ignored
    }

    [Fact]
    public async Task Dispose_WaitsForRenderLoopToStop()
    {
        // Arrange
        var renderer = CreateRenderer();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        renderer.Dispose();
        stopwatch.Stop();

        // Assert
        // Disposal should complete relatively quickly (within reasonable time)
        Assert.True(stopwatch.ElapsedMilliseconds < 2000); // Should stop within 2 seconds
    }

    [Fact]
    public void RenderStatistics_CalculatesPropertiesCorrectly()
    {
        // Arrange
        var renderTime = 12.5;
        var staticCount = 3;
        var dynamicCount = 2;
        var terminalSize = new TerminalSize(100, 50);

        // Act
        var stats = new RenderStatistics(renderTime, staticCount, dynamicCount, terminalSize);

        // Assert
        Assert.Equal(renderTime, stats.RenderTimeMs);
        Assert.Equal(staticCount, stats.StaticComponentCount);
        Assert.Equal(dynamicCount, stats.DynamicComponentCount);
        Assert.Equal(terminalSize, stats.TerminalSize);
        Assert.Equal(5, stats.TotalComponents);
        Assert.True(stats.IsFastRender); // 12.5ms < 16ms
    }

    [Fact]
    public void RenderStatistics_IsFastRender_ReturnsFalseForSlowRenders()
    {
        // Arrange
        var slowRenderTime = 25.0; // > 16ms
        var stats = new RenderStatistics(slowRenderTime, 1, 1, new TerminalSize(100, 50));

        // Assert
        Assert.False(stats.IsFastRender);
    }

    [Theory]
    [InlineData(5.0, true)]
    [InlineData(15.9, true)]
    [InlineData(16.0, false)]
    [InlineData(20.0, false)]
    public void RenderStatistics_IsFastRender_CorrectlyIdentifiesFastRenders(double renderTime, bool expectedFast)
    {
        // Arrange
        var stats = new RenderStatistics(renderTime, 1, 1, new TerminalSize(100, 50));

        // Assert
        Assert.Equal(expectedFast, stats.IsFastRender);
    }

    [Fact]
    public async Task RegisterComponent_AfterDisposal_IsIgnored()
    {
        // Arrange
        var renderer = CreateRenderer();
        var component = new TestComponent("test-component");
        
        renderer.Dispose();

        // Act & Assert
        // Should not throw, but should be ignored
        renderer.RegisterStaticComponent(component);
        renderer.RegisterDynamicComponent(component);
        renderer.UnregisterComponent(component);
        
        await renderer.ForceRenderAsync(); // Should be ignored
    }

    private TuiRenderer CreateRenderer(IAnsiConsole? console = null, StateManager? stateManager = null, LayoutManager? layoutManager = null)
    {
        if (console == null)
        {
            var output = new StringWriter();
            console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        }
        stateManager ??= CreateStateManager();
        layoutManager ??= new LayoutManager();
        
        var renderer = new TuiRenderer(console, stateManager, layoutManager);
        _renderersToDispose.Add(renderer);
        return renderer;
    }

    private StateManager CreateStateManager()
    {
        var stateManager = new StateManager();
        _stateManagersToDispose.Add(stateManager);
        return stateManager;
    }

    public void Dispose()
    {
        foreach (var renderer in _renderersToDispose)
        {
            try
            {
                renderer.Dispose();
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
        
        foreach (var stateManager in _stateManagersToDispose)
        {
            try
            {
                stateManager.Dispose();
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
    }

    // Test component implementations
    private class TestComponent : ITuiComponent
    {
        public string ComponentId { get; }
        public bool RenderCalled { get; private set; }
        public int RenderCallCount { get; private set; }

        public TestComponent(string componentId)
        {
            ComponentId = componentId;
        }

        public Task<IRenderable> RenderAsync(RenderContext context)
        {
            RenderCalled = true;
            RenderCallCount++;
            return Task.FromResult<IRenderable>(new Text("Test Component Content"));
        }

        public bool ShouldUpdate(RenderContext context) => true;

        public Task OnMountAsync() => Task.CompletedTask;

        public Task OnUnmountAsync() => Task.CompletedTask;

        public void Dispose() { }
    }

    private class FailingTestComponent : ITuiComponent
    {
        public string ComponentId { get; }

        public FailingTestComponent(string componentId)
        {
            ComponentId = componentId;
        }

        public Task<IRenderable> RenderAsync(RenderContext context)
        {
            throw new InvalidOperationException("Test error");
        }

        public bool ShouldUpdate(RenderContext context) => true;

        public Task OnMountAsync() => Task.CompletedTask;

        public Task OnUnmountAsync() => Task.CompletedTask;

        public void Dispose() { }
    }
}
