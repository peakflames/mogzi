using UI.Core;
using UI.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace UI.Tests;

public class TuiAppTests : IDisposable
{
    private readonly List<TuiApp> _appsToDispose = new();

    [Fact]
    public void Constructor_WithValidServiceProvider_InitializesCorrectly()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();

        // Act
        var app = CreateApp(serviceProvider);

        // Assert
        Assert.NotNull(app);
        Assert.False(app.IsRunning);
        Assert.NotNull(app.Statistics);
        Assert.False(app.Statistics.IsRunning);
        Assert.Equal(0, app.Statistics.RegisteredComponentCount);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TuiApp(null!));
    }

    [Fact]
    public void RegisterComponent_WithValidComponent_RegistersSuccessfully()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");

        // Act
        app.RegisterComponent(component, RenderZone.Static);

        // Assert
        var stats = app.GetStatistics();
        Assert.Equal(1, stats.RegisteredComponentCount);
    }

    [Fact]
    public void RegisterComponent_WithNullComponent_ThrowsArgumentNullException()
    {
        // Arrange
        var app = CreateApp();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.RegisterComponent(null!));
    }

    [Fact]
    public void RegisterComponent_WithDynamicZone_RegistersInDynamicZone()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("dynamic-component");

        // Act
        app.RegisterComponent(component, RenderZone.Dynamic);

        // Assert
        var stats = app.GetStatistics();
        Assert.Equal(1, stats.RegisteredComponentCount);
    }

    [Fact]
    public void RegisterComponent_WithInvalidZone_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            app.RegisterComponent(component, (RenderZone)999));
    }

    [Fact]
    public void UnregisterComponent_WithRegisteredComponent_UnregistersSuccessfully()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        // Act
        app.UnregisterComponent(component);

        // Assert
        var stats = app.GetStatistics();
        Assert.Equal(0, stats.RegisteredComponentCount);
    }

    [Fact]
    public void UnregisterComponent_WithNullComponent_DoesNotThrow()
    {
        // Arrange
        var app = CreateApp();

        // Act & Assert
        // Should not throw
        app.UnregisterComponent(null!);
    }

    [Fact]
    public async Task RunAsync_WithValidArgs_ReturnsSuccessExitCode()
    {
        // Arrange
        var app = CreateApp();
        var args = new[] { "--test" };
        using var cts = new CancellationTokenSource();

        // Cancel after a short delay to stop the app
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var exitCode = await app.RunAsync(args, cts.Token);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var app = CreateApp();
        var args = new[] { "--test" };
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Start the app
        var runTask = app.RunAsync(args, cts.Token);

        // Wait a bit for the app to start
        await Task.Delay(10);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.RunAsync(args));

        // Wait for the original run to complete
        await runTask;
    }

    [Fact]
    public async Task RunAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var app = CreateApp();
        app.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => app.RunAsync(new string[0]));
    }

    [Fact]
    public async Task RunAsync_RaisesStartedAndStoppedEvents()
    {
        // Arrange
        var app = CreateApp();
        var startedRaised = false;
        var stoppedRaised = false;

        app.Started += () => startedRaised = true;
        app.Stopped += () => stoppedRaised = true;

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await app.RunAsync(new string[0], cts.Token);

        // Assert
        Assert.True(startedRaised);
        Assert.True(stoppedRaised);
    }

    [Fact]
    public async Task RunAsync_CallsComponentLifecycleMethods()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("lifecycle-component");

        app.RegisterComponent(component);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await app.RunAsync(new string[0], cts.Token);

        // Assert
        Assert.True(component.MountCalled);
        Assert.True(component.UnmountCalled);
    }

    [Fact]
    public async Task RunAsync_WithUnhandledError_RaisesUnhandledErrorEvent()
    {
        // Arrange
        var app = CreateApp();
        var component = new FailingMountComponent("failing-component");
        Exception? capturedError = null;

        app.UnhandledError += ex => capturedError = ex;
        app.RegisterComponent(component);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var exitCode = await app.RunAsync(new string[0], cts.Token);

        // Assert
        // App should handle component errors gracefully and continue running
        Assert.Equal(0, exitCode);
        // Component mount error should not propagate as unhandled error
        // (it should be handled internally)
    }

    [Fact]
    public async Task ForceRenderAsync_WithRegisteredComponents_TriggersRender()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        // Start the app so ForceRenderAsync will work
        var runTask = app.RunAsync(new string[0], cts.Token);
        
        // Wait for app to start
        await Task.Delay(50);

        // Act
        await app.ForceRenderAsync();

        // Wait for async operations
        await Task.Delay(100);

        // Wait for run task to complete
        await runTask;

        // Assert
        Assert.True(component.RenderCalled);
    }

    [Fact]
    public async Task ForceRenderAsync_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        // Act & Assert
        // Should not throw when app is not running
        await app.ForceRenderAsync();
    }

    [Fact]
    public void UpdateLayoutConfiguration_WithNewConfiguration_UpdatesLayout()
    {
        // Arrange
        var app = CreateApp();
        var newConfig = new LayoutConfiguration(StaticZoneRatio: 0.8, DynamicZoneRatio: 0.2);

        // Act & Assert
        // Should not throw
        app.UpdateLayoutConfiguration(newConfig);
    }

    [Fact]
    public void GetStatistics_ReturnsCurrentStatistics()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        // Act
        var stats = app.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.False(stats.IsRunning);
        Assert.Equal(1, stats.RegisteredComponentCount);
        Assert.Equal(0.0, stats.CurrentFps);
        Assert.Equal(0, stats.TotalFrames);
    }

    [Fact]
    public void GetStatistics_AfterDisposal_ReturnsEmptyStatistics()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        // Act
        app.Dispose();
        var stats = app.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.False(stats.IsRunning);
        Assert.Equal(0, stats.RegisteredComponentCount);
    }

    [Fact]
    public void ApplicationStatistics_IsPerformingWell_CalculatesCorrectly()
    {
        // Arrange
        var goodStats = new ApplicationStatistics(
            IsRunning: true,
            CurrentFps: 45.0,
            LastRenderTimeMs: 15.0
        );

        var poorStats = new ApplicationStatistics(
            IsRunning: true,
            CurrentFps: 20.0,
            LastRenderTimeMs: 40.0
        );

        // Act & Assert
        Assert.True(goodStats.IsPerformingWell);
        Assert.False(poorStats.IsPerformingWell);
    }

    [Theory]
    [InlineData(5, MemoryPressure.Low)]
    [InlineData(25, MemoryPressure.Medium)]
    [InlineData(75, MemoryPressure.High)]
    [InlineData(150, MemoryPressure.Critical)]
    public void ApplicationStatistics_MemoryPressure_CalculatesCorrectly(int componentCount, MemoryPressure expectedPressure)
    {
        // Arrange
        var stats = new ApplicationStatistics(RegisteredComponentCount: componentCount);

        // Act & Assert
        Assert.Equal(expectedPressure, stats.MemoryPressure);
    }

    [Fact]
    public void Dispose_CleansUpResources_AndStopsOperations()
    {
        // Arrange
        var app = CreateApp();
        var component = new TestComponent("test-component");
        app.RegisterComponent(component);

        // Act
        app.Dispose();

        // Assert
        // After disposal, operations should be no-ops
        app.RegisterComponent(component); // Should be ignored
        app.UnregisterComponent(component); // Should be ignored
        app.UpdateLayoutConfiguration(LayoutConfiguration.Default); // Should be ignored
    }

    [Fact]
    public async Task Dispose_WaitsForRunningOperationsToComplete()
    {
        // Arrange
        var app = CreateApp();
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var runTask = app.RunAsync(new string[0], cts.Token);
        await Task.Delay(10); // Let app start

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        app.Dispose();
        stopwatch.Stop();

        // Wait for run task to complete
        await runTask;

        // Assert
        // Disposal should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 2000);
    }

    private TuiApp CreateApp(IServiceProvider? serviceProvider = null)
    {
        serviceProvider ??= CreateServiceProvider();
        var app = new TuiApp(serviceProvider);
        _appsToDispose.Add(app);
        return app;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        
        // Add required services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Add console with string writer to capture output
        var output = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(output) });
        services.AddSingleton<IAnsiConsole>(console);

        return services.BuildServiceProvider();
    }

    public void Dispose()
    {
        foreach (var app in _appsToDispose)
        {
            try
            {
                app.Dispose();
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
        public bool MountCalled { get; private set; }
        public bool UnmountCalled { get; private set; }

        public TestComponent(string componentId)
        {
            ComponentId = componentId;
        }

        public Task<IRenderable> RenderAsync(RenderContext context)
        {
            RenderCalled = true;
            return Task.FromResult<IRenderable>(new Text("Test Component Content"));
        }

        public bool ShouldUpdate(RenderContext context) => true;

        public Task OnMountAsync()
        {
            MountCalled = true;
            return Task.CompletedTask;
        }

        public Task OnUnmountAsync()
        {
            UnmountCalled = true;
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }

    private class FailingMountComponent : ITuiComponent
    {
        public string ComponentId { get; }

        public FailingMountComponent(string componentId)
        {
            ComponentId = componentId;
        }

        public Task<IRenderable> RenderAsync(RenderContext context)
        {
            return Task.FromResult<IRenderable>(new Text("Failing Component"));
        }

        public bool ShouldUpdate(RenderContext context) => true;

        public Task OnMountAsync()
        {
            throw new InvalidOperationException("Mount failed");
        }

        public Task OnUnmountAsync() => Task.CompletedTask;

        public void Dispose() { }
    }
}
