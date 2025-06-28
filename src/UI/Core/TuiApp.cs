namespace UI.Core;

/// <summary>
/// Main application class for the Terminal User Interface.
/// </summary>
public sealed class TuiApp : IAsyncDisposable, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TuiApp> _logger;
    private readonly StateManager _stateManager;
    private readonly TuiRenderer _renderer;
    private readonly LayoutManager _layoutManager;
    private readonly IAnsiConsole _console;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly List<ITuiComponent> _registeredComponents = new();
    private readonly AdvancedKeyboardHandler _keyboardHandler;
    private bool _isRunning = false;
    private bool _isDisposed = false;

    /// <summary>
    /// Gets whether the application is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the current application statistics.
    /// </summary>
    public ApplicationStatistics Statistics { get; private set; } = new();

    /// <summary>
    /// Event raised when the application starts.
    /// </summary>
    public event Action? Started;

    /// <summary>
    /// Event raised when the application stops.
    /// </summary>
    public event Action? Stopped;

    /// <summary>
    /// Event raised when an unhandled error occurs.
    /// </summary>
    public event Action<Exception>? UnhandledError;

    /// <summary>
    /// Initializes a new instance of TuiApp.
    /// </summary>
    public TuiApp(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetRequiredService<ILogger<TuiApp>>();
        _console = serviceProvider.GetService<IAnsiConsole>() ?? AnsiConsole.Console;
        
        _cancellationTokenSource = new CancellationTokenSource();
        _stateManager = new StateManager(_serviceProvider.GetService<ILogger<StateManager>>());
        _layoutManager = new LayoutManager(null, _serviceProvider.GetService<ILogger<LayoutManager>>());
        _renderer = new TuiRenderer(_console, _stateManager, _layoutManager, 
            _serviceProvider.GetService<ILogger<TuiRenderer>>());

        // Initialize the advanced keyboard handler
        _keyboardHandler = new AdvancedKeyboardHandler(_serviceProvider.GetService<ILogger<AdvancedKeyboardHandler>>());
        
        // Subscribe to renderer events
        _renderer.RenderCompleted += OnRenderCompleted;
        
        // Subscribe to keyboard events
        _keyboardHandler.KeyPressed += OnKeyPressed;
        _keyboardHandler.KeyCombinationPressed += OnKeyCombinationPressed;
        _keyboardHandler.CharacterTyped += OnCharacterTyped;
        
        // Register enhanced key bindings
        RegisterEnhancedKeyBindings();

        _logger.LogDebug("TuiApp initialized with advanced keyboard handling");
    }

    /// <summary>
    /// Runs the TUI application with the specified arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>Exit code (0 for success, non-zero for error).</returns>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TuiApp));

        if (_isRunning)
            throw new InvalidOperationException("Application is already running");

        try
        {
            _logger.LogInformation("Starting TUI application");
            
            // Combine cancellation tokens
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token);

            // Initialize the application
            await InitializeAsync(args, combinedCts.Token);

            // Mark as running
            _isRunning = true;
            Started?.Invoke();

            // Run the main application loop
            var exitCode = await RunMainLoopAsync(combinedCts.Token);

            _logger.LogInformation("TUI application completed with exit code {ExitCode}", exitCode);
            return exitCode;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("TUI application cancelled by user");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in TUI application");
            UnhandledError?.Invoke(ex);
            return 1;
        }
        finally
        {
            _isRunning = false;
            Stopped?.Invoke();
            await ShutdownAsync();
        }
    }

    /// <summary>
    /// Registers a component with the application.
    /// </summary>
    /// <param name="component">The component to register.</param>
    /// <param name="zone">The render zone to register the component with.</param>
    public void RegisterComponent(ITuiComponent component, RenderZone zone = RenderZone.Dynamic)
    {
        if (_isDisposed) return;
        if (component == null) throw new ArgumentNullException(nameof(component));

        _registeredComponents.Add(component);

        switch (zone)
        {
            case RenderZone.Static:
                _renderer.RegisterStaticComponent(component);
                break;
            case RenderZone.Dynamic:
                _renderer.RegisterDynamicComponent(component);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(zone));
        }

        _logger.LogDebug("Registered component {ComponentId} in {Zone} zone", 
            component.ComponentId, zone);
    }

    /// <summary>
    /// Unregisters a component from the application.
    /// </summary>
    /// <param name="component">The component to unregister.</param>
    public void UnregisterComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        if (component == null) return;

        _registeredComponents.Remove(component);
        _renderer.UnregisterComponent(component);

        _logger.LogDebug("Unregistered component {ComponentId}", component.ComponentId);
    }

    /// <summary>
    /// Forces a complete re-render of the application.
    /// </summary>
    public async Task ForceRenderAsync()
    {
        if (_isDisposed || !_isRunning) return;

        try
        {
            await _renderer.ForceRenderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forced render");
        }
    }

    /// <summary>
    /// Updates the layout configuration.
    /// </summary>
    /// <param name="configuration">The new layout configuration.</param>
    public void UpdateLayoutConfiguration(LayoutConfiguration configuration)
    {
        if (_isDisposed) return;

        _layoutManager.UpdateConfiguration(configuration);
        _logger.LogDebug("Layout configuration updated");
    }

    /// <summary>
    /// Gets the current application statistics.
    /// </summary>
    public ApplicationStatistics GetStatistics()
    {
        if (_isDisposed) return new ApplicationStatistics();

        return Statistics with
        {
            IsRunning = _isRunning,
            RegisteredComponentCount = _registeredComponents.Count,
            CurrentFps = _renderer.CurrentFps,
            TotalFrames = _renderer.TotalFrames,
            LastRenderTimeMs = _renderer.LastRenderTimeMs,
            StateManagerStats = _stateManager.GetStatistics(),
            TerminalSize = TerminalSize.Current,
            KeyboardStatistics = _keyboardHandler.GetStatistics()
        };
    }

    /// <summary>
    /// Initializes the application.
    /// </summary>
    private async Task InitializeAsync(string[] args, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing TUI application");

        // Setup console
        _console.Clear();
        
        // Initialize components
        foreach (var component in _registeredComponents)
        {
            try
            {
                await component.OnMountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error mounting component {ComponentId}", component.ComponentId);
            }
        }

        // Force initial render
        await _renderer.ForceRenderAsync();

        _logger.LogDebug("TUI application initialized");
    }

    /// <summary>
    /// Runs the main application loop.
    /// </summary>
    private async Task<int> RunMainLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting main application loop");

        try
        {
            // Start the advanced keyboard event handling task
            var keyboardTask = _keyboardHandler.StartAsync(cancellationToken);
            
            // Main loop - handle statistics and other background tasks
            while (!cancellationToken.IsCancellationRequested)
            {
                // Update statistics
                Statistics = GetStatistics();

                // Wait a bit before next iteration
                await Task.Delay(100, cancellationToken);
            }

            // Stop keyboard handler and wait for completion
            await _keyboardHandler.StopAsync();
            await keyboardTask;

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in main application loop");
            return 1;
        }
    }

    /// <summary>
    /// Registers enhanced key bindings for improved functionality.
    /// </summary>
    private void RegisterEnhancedKeyBindings()
    {
        // Global shortcuts
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.C, ConsoleModifiers.Control, args =>
        {
            _logger.LogInformation("Ctrl+C detected, requesting shutdown");
            _cancellationTokenSource.Cancel();
            args.Handled = true;
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.L, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+L detected, clearing screen");
            _console.Clear();
            _ = ForceRenderAsync(); // Fire and forget
            args.Handled = true;
        });

        // Input shortcuts for command history navigation
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.P, ConsoleModifiers.Control, args =>
        {
            var inputComponent = GetInputComponent();
            if (inputComponent != null)
            {
                inputComponent.NavigateCommandHistory(up: true);
                _ = ForceRenderAsync(); // Fire and forget
                args.Handled = true;
            }
        });

        _keyboardHandler.RegisterKeyBinding(ConsoleKey.N, ConsoleModifiers.Control, args =>
        {
            var inputComponent = GetInputComponent();
            if (inputComponent != null)
            {
                inputComponent.NavigateCommandHistory(up: false);
                _ = ForceRenderAsync(); // Fire and forget
                args.Handled = true;
            }
        });

        // External editor placeholder (Ctrl+E)
        _keyboardHandler.RegisterKeyBinding(ConsoleKey.E, ConsoleModifiers.Control, args =>
        {
            _logger.LogDebug("Ctrl+E detected - external editor feature placeholder");
            // TODO: Implement external editor integration in future phases
            args.Handled = true;
        });

        _logger.LogDebug("Enhanced key bindings registered");
    }

    /// <summary>
    /// Handles general key press events.
    /// </summary>
    private async void OnKeyPressed(object? sender, KeyPressEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            var inputComponent = GetInputComponent();
            if (inputComponent == null) return;

            // Handle navigation and editing keys
            switch (e.Key)
            {
                case ConsoleKey.Enter:
                    inputComponent.SubmitCurrentInput();
                    e.Handled = true;
                    break;

                case ConsoleKey.UpArrow:
                    inputComponent.NavigateCommandHistory(up: true);
                    e.Handled = true;
                    break;

                case ConsoleKey.DownArrow:
                    inputComponent.NavigateCommandHistory(up: false);
                    e.Handled = true;
                    break;

                case ConsoleKey.LeftArrow:
                    var extendLeft = e.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    inputComponent.MoveCursorLeft(extendLeft);
                    e.Handled = true;
                    break;

                case ConsoleKey.RightArrow:
                    var extendRight = e.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    inputComponent.MoveCursorRight(extendRight);
                    e.Handled = true;
                    break;

                case ConsoleKey.Home:
                    var extendHome = e.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    inputComponent.MoveCursorToStart(extendHome);
                    e.Handled = true;
                    break;

                case ConsoleKey.End:
                    var extendEnd = e.Modifiers.HasFlag(ConsoleModifiers.Shift);
                    inputComponent.MoveCursorToEnd(extendEnd);
                    e.Handled = true;
                    break;

                case ConsoleKey.Backspace:
                    inputComponent.DeleteCharacterBefore();
                    e.Handled = true;
                    break;

                case ConsoleKey.Delete:
                    inputComponent.DeleteCharacterAfter();
                    e.Handled = true;
                    break;

                case ConsoleKey.Escape:
                    inputComponent.ClearCurrentInput();
                    e.Handled = true;
                    break;
            }

            if (e.Handled)
            {
                await ForceRenderAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key press: {Key}", e.Key);
        }
    }

    /// <summary>
    /// Handles key combination events.
    /// </summary>
    private async void OnKeyCombinationPressed(object? sender, KeyCombinationEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            // Key combinations are handled by registered bindings
            // This event is for any unhandled combinations
            _logger.LogDebug("Unhandled key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key combination: {Key} + {Modifiers}", e.Key, e.Modifiers);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles character typed events.
    /// </summary>
    private async void OnCharacterTyped(object? sender, CharacterTypedEventArgs e)
    {
        if (e.Handled) return;

        try
        {
            var inputComponent = GetInputComponent();
            if (inputComponent != null)
            {
                inputComponent.InsertCharacter(e.Character);
                await ForceRenderAsync();
                e.Handled = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character typed: {Character}", e.Character);
        }
    }

    /// <summary>
    /// Gets the current InputComponent from registered components.
    /// </summary>
    private InputComponent? GetInputComponent()
    {
        return _registeredComponents
            .OfType<AppComponent>()
            .FirstOrDefault()?
            .GetInputComponent();
    }

    /// <summary>
    /// Shuts down the application gracefully.
    /// </summary>
    private async Task ShutdownAsync()
    {
        _logger.LogDebug("Shutting down TUI application");

        try
        {
            // Stop keyboard handler
            await _keyboardHandler.StopAsync();

            // Unmount all components
            foreach (var component in _registeredComponents)
            {
                try
                {
                    await component.OnUnmountAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error unmounting component {ComponentId}", component.ComponentId);
                }
            }

            // Clear console
            _console.Clear();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }

        _logger.LogDebug("TUI application shutdown complete");
    }

    /// <summary>
    /// Called when a render cycle completes.
    /// </summary>
    private void OnRenderCompleted(RenderStatistics statistics)
    {
        // Update application statistics based on render statistics
        Statistics = Statistics with
        {
            LastRenderStatistics = statistics
        };
    }

    /// <summary>
    /// Disposes the application and cleans up resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        // Cancel any running operations
        _cancellationTokenSource.Cancel();

        // Dispose keyboard handler
        _keyboardHandler?.Dispose();

        // Dispose components
        foreach (var component in _registeredComponents)
        {
            try
            {
                component.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing component {ComponentId}", component.ComponentId);
            }
        }
        _registeredComponents.Clear();

        // Dispose services
        if (_renderer != null)
        {
            await _renderer.DisposeAsync();
        }
        _stateManager?.Dispose();
        _cancellationTokenSource?.Dispose();

        // Clear event subscriptions
        Started = null;
        Stopped = null;
        UnhandledError = null;

        _logger?.LogDebug("TuiApp disposed");

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Enumeration of render zones.
/// </summary>
public enum RenderZone
{
    /// <summary>
    /// Static render zone for components that change infrequently.
    /// </summary>
    Static,

    /// <summary>
    /// Dynamic render zone for components that change frequently.
    /// </summary>
    Dynamic
}

/// <summary>
/// Contains application statistics and performance metrics.
/// </summary>
public sealed record ApplicationStatistics(
    bool IsRunning = false,
    int RegisteredComponentCount = 0,
    double CurrentFps = 0.0,
    long TotalFrames = 0,
    double LastRenderTimeMs = 0.0,
    IReadOnlyDictionary<string, object>? StateManagerStats = null,
    TerminalSize? TerminalSize = null,
    RenderStatistics? LastRenderStatistics = null,
    KeyboardStatistics? KeyboardStatistics = null)
{
    /// <summary>
    /// Gets whether the application is performing well.
    /// </summary>
    public bool IsPerformingWell => CurrentFps > 30 && LastRenderTimeMs < 32;

    /// <summary>
    /// Gets the memory pressure level based on component count.
    /// </summary>
    public MemoryPressure MemoryPressure => RegisteredComponentCount switch
    {
        < 10 => MemoryPressure.Low,
        < 50 => MemoryPressure.Medium,
        < 100 => MemoryPressure.High,
        _ => MemoryPressure.Critical
    };
}

/// <summary>
/// Enumeration of memory pressure levels.
/// </summary>
public enum MemoryPressure
{
    Low,
    Medium,
    High,
    Critical
}
