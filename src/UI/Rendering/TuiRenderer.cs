namespace UI.Rendering;

/// <summary>
/// Core rendering engine for the TUI application.
/// </summary>
public sealed class TuiRenderer : IAsyncDisposable
{
    private readonly IAnsiConsole _console;
    private readonly StateManager _stateManager;
    private readonly StaticRenderZone _staticZone;
    private readonly DynamicRenderZone _dynamicZone;
    private readonly LayoutManager _layoutManager;
    private readonly ILogger<TuiRenderer>? _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _renderLoop;
    private bool _isDisposed = false;

    /// <summary>
    /// Gets the current frames per second of the rendering loop.
    /// </summary>
    public double CurrentFps { get; private set; }

    /// <summary>
    /// Gets the total number of frames rendered.
    /// </summary>
    public long TotalFrames { get; private set; }

    /// <summary>
    /// Gets the last render time in milliseconds.
    /// </summary>
    public double LastRenderTimeMs { get; private set; }

    /// <summary>
    /// Event raised when a render cycle completes.
    /// </summary>
    public event Action<RenderStatistics>? RenderCompleted;

    /// <summary>
    /// Initializes a new instance of TuiRenderer.
    /// </summary>
    public TuiRenderer(
        IAnsiConsole console,
        StateManager stateManager,
        LayoutManager layoutManager,
        ILogger<TuiRenderer>? logger = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _layoutManager = layoutManager ?? throw new ArgumentNullException(nameof(layoutManager));
        _logger = logger;

        _staticZone = new StaticRenderZone(logger);
        _dynamicZone = new DynamicRenderZone(logger);
        _cancellationTokenSource = new CancellationTokenSource();

        // Subscribe to state changes
        _stateManager.StateChangesReady += OnStateChangesReady;

        // Start the render loop
        _renderLoop = Task.Run(RenderLoopAsync, _cancellationTokenSource.Token);

        _logger?.LogDebug("TuiRenderer initialized");
    }

    /// <summary>
    /// Registers a component with the static render zone.
    /// </summary>
    public void RegisterStaticComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        _staticZone.RegisterComponent(component);
        _logger?.LogTrace("Registered static component {ComponentId}", component.ComponentId);
    }

    /// <summary>
    /// Registers a component with the dynamic render zone.
    /// </summary>
    public void RegisterDynamicComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        _dynamicZone.RegisterComponent(component);
        _logger?.LogTrace("Registered dynamic component {ComponentId}", component.ComponentId);
    }

    /// <summary>
    /// Unregisters a component from both render zones.
    /// </summary>
    public void UnregisterComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        _staticZone.UnregisterComponent(component);
        _dynamicZone.UnregisterComponent(component);
        _logger?.LogTrace("Unregistered component {ComponentId}", component.ComponentId);
    }

    /// <summary>
    /// Forces an immediate render of all components.
    /// </summary>
    public async Task ForceRenderAsync()
    {
        if (_isDisposed) return;

        try
        {
            await RenderFrameAsync(forceRender: true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during forced render");
        }
    }

    /// <summary>
    /// Main render loop that runs continuously.
    /// </summary>
    private async Task RenderLoopAsync()
    {
        var frameTimer = System.Diagnostics.Stopwatch.StartNew();
        var fpsTimer = System.Diagnostics.Stopwatch.StartNew();
        var frameCount = 0;

        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var renderStart = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // Only render if there are pending changes or it's been too long since last render
                    if (_stateManager.HasPendingChanges || frameTimer.ElapsedMilliseconds > 1000)
                    {
                        await RenderFrameAsync();
                        frameTimer.Restart();
                        frameCount++;
                        TotalFrames++;
                    }

                    // Calculate FPS every second
                    if (fpsTimer.ElapsedMilliseconds >= 1000)
                    {
                        CurrentFps = frameCount / (fpsTimer.ElapsedMilliseconds / 1000.0);
                        frameCount = 0;
                        fpsTimer.Restart();
                    }

                    LastRenderTimeMs = renderStart.Elapsed.TotalMilliseconds;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in render loop");
                }

                // Target 60 FPS (16.67ms per frame)
                var targetFrameTime = TimeSpan.FromMilliseconds(16.67);
                var elapsed = renderStart.Elapsed;
                if (elapsed < targetFrameTime)
                {
                    await Task.Delay(targetFrameTime - elapsed, _cancellationTokenSource.Token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shutting down
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Fatal error in render loop");
        }

        _logger?.LogDebug("Render loop stopped");
    }

    /// <summary>
    /// Renders a single frame.
    /// </summary>
    private async Task RenderFrameAsync(bool forceRender = false)
    {
        if (_isDisposed) return;

        var renderStart = System.Diagnostics.Stopwatch.StartNew();
        var terminalSize = TerminalSize.Current;
        var layout = _layoutManager.CalculateLayout(terminalSize);

        try
        {
            // Clear the console if this is a forced render
            if (forceRender)
            {
                _console.Clear();
            }

            // Create render contexts for each zone
            var staticContext = new RenderContext(
                layout.StaticZoneConstraints,
                terminalSize,
                forceRender
            );

            var dynamicContext = new RenderContext(
                layout.DynamicZoneConstraints,
                terminalSize,
                forceRender
            );

            // Render both zones
            var staticRenderable = await _staticZone.RenderAsync(staticContext);
            var dynamicRenderable = await _dynamicZone.RenderAsync(dynamicContext);

            // Combine the renderables
            var combinedRenderable = new Rows(staticRenderable, dynamicRenderable);

            // Render to console
            _console.Write(combinedRenderable);

            // Clear pending changes after successful render
            _stateManager.ClearPendingChanges();

            // Raise render completed event
            var statistics = new RenderStatistics(
                renderStart.Elapsed.TotalMilliseconds,
                _staticZone.ComponentCount,
                _dynamicZone.ComponentCount,
                terminalSize
            );

            RenderCompleted?.Invoke(statistics);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering frame");
        }
    }

    /// <summary>
    /// Called when state changes are ready to be processed.
    /// </summary>
    private void OnStateChangesReady()
    {
        // The render loop will pick up the changes automatically
        _logger?.LogTrace("State changes ready for rendering");
    }

    /// <summary>
    /// Disposes the renderer and stops the render loop.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // Stop the render loop
        _cancellationTokenSource.Cancel();

        try
        {
            await _renderLoop;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error waiting for render loop to stop");
        }

        // Unsubscribe from state changes
        _stateManager.StateChangesReady -= OnStateChangesReady;

        // Dispose render zones
        _staticZone.Dispose();
        _dynamicZone.Dispose();

        // Dispose cancellation token
        _cancellationTokenSource.Dispose();

        // Clear event subscriptions
        RenderCompleted = null;

        _logger?.LogDebug("TuiRenderer disposed");

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Contains statistics about a render cycle.
/// </summary>
public sealed record RenderStatistics(
    double RenderTimeMs,
    int StaticComponentCount,
    int DynamicComponentCount,
    TerminalSize TerminalSize)
{
    /// <summary>
    /// Gets the total number of components rendered.
    /// </summary>
    public int TotalComponents => StaticComponentCount + DynamicComponentCount;

    /// <summary>
    /// Gets whether the render was fast (under 16ms for 60 FPS).
    /// </summary>
    public bool IsFastRender => RenderTimeMs < 16.0;
}
