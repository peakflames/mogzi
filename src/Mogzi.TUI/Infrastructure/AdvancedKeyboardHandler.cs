namespace Mogzi.TUI.Infrastructure;

/// <summary>
/// Advanced keyboard input handler with event-driven architecture and key combination support.
/// Replaces polling-based input with proper event handling for better responsiveness.
/// </summary>
public sealed class AdvancedKeyboardHandler : IDisposable
{
    private readonly ILogger<AdvancedKeyboardHandler>? _logger;
    private readonly Dictionary<KeyBinding, Action<KeyPressEventArgs>> _keyBindings = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _inputTask;
    private bool _isDisposed = false;

    /// <summary>
    /// Event raised when a key is pressed.
    /// </summary>
    public event EventHandler<KeyPressEventArgs>? KeyPressed;

    /// <summary>
    /// Event raised when a key combination is pressed.
    /// </summary>
    public event EventHandler<KeyCombinationEventArgs>? KeyCombinationPressed;

    /// <summary>
    /// Event raised when a character is typed (excludes control keys).
    /// </summary>
    public event EventHandler<CharacterTypedEventArgs>? CharacterTyped;

    /// <summary>
    /// Gets whether the keyboard handler is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Gets the current keyboard input statistics.
    /// </summary>
    public KeyboardStatistics Statistics { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of AdvancedKeyboardHandler.
    /// </summary>
    public AdvancedKeyboardHandler(ILogger<AdvancedKeyboardHandler>? logger = null)
    {
        _logger = logger;
        RegisterDefaultKeyBindings();
        _logger?.LogTrace("AdvancedKeyboardHandler initialized");
    }

    /// <summary>
    /// Starts the keyboard input handling.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("Keyboard handler is already running");
        }

        _logger?.LogTrace("Starting advanced keyboard input handling");

        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token);

        IsRunning = true;
        _inputTask = HandleKeyboardInputAsync(combinedCts.Token);

        try
        {
            await _inputTask;
        }
        finally
        {
            IsRunning = false;
            _logger?.LogTrace("Advanced keyboard input handling stopped");
        }
    }

    /// <summary>
    /// Stops the keyboard input handling.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _logger?.LogTrace("Stopping advanced keyboard input handling");
        _cancellationTokenSource.Cancel();

        if (_inputTask != null)
        {
            try
            {
                await _inputTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }
    }

    /// <summary>
    /// Registers a key binding for a specific key combination.
    /// </summary>
    public void RegisterKeyBinding(ConsoleKey key, ConsoleModifiers modifiers, Action<KeyPressEventArgs> handler)
    {
        if (_isDisposed)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(handler);

        var binding = new KeyBinding(key, modifiers);
        _keyBindings[binding] = handler;

        _logger?.LogTrace("Registered key binding: {Key} + {Modifiers}", key, modifiers);
    }

    /// <summary>
    /// Registers a key binding for a single key without modifiers.
    /// </summary>
    public void RegisterKeyBinding(ConsoleKey key, Action<KeyPressEventArgs> handler)
    {
        RegisterKeyBinding(key, ConsoleModifiers.None, handler);
    }

    /// <summary>
    /// Unregisters a key binding.
    /// </summary>
    public void UnregisterKeyBinding(ConsoleKey key, ConsoleModifiers modifiers = ConsoleModifiers.None)
    {
        if (_isDisposed)
        {
            return;
        }

        var binding = new KeyBinding(key, modifiers);
        if (_keyBindings.Remove(binding))
        {
            _logger?.LogTrace("Unregistered key binding: {Key} + {Modifiers}", key, modifiers);
        }
    }

    /// <summary>
    /// Handles keyboard input in an event-driven manner.
    /// </summary>
    private async Task HandleKeyboardInputAsync(CancellationToken cancellationToken)
    {
        _logger?.LogTrace("Starting keyboard input loop");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if a key is available without blocking
                if (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true); // true = don't display the key
                    await ProcessKeyInputAsync(keyInfo);

                    // Update statistics
                    Statistics = Statistics with
                    {
                        TotalKeysProcessed = Statistics.TotalKeysProcessed + 1,
                        LastKeyPressTime = DateTime.UtcNow
                    };
                }
                else
                {
                    // Small delay to prevent busy waiting, but more responsive than before
                    await Task.Delay(8, cancellationToken); // ~120 FPS polling rate for better responsiveness
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in keyboard input handling");
            throw;
        }

        _logger?.LogTrace("Keyboard input loop stopped");
    }

    /// <summary>
    /// Processes a single key input event.
    /// </summary>
    private async Task ProcessKeyInputAsync(ConsoleKeyInfo keyInfo)
    {
        _logger?.LogTrace("Processing KeyInput: Key={Key}, KeyChar='{KeyChar}', Modifiers={Modifiers}", keyInfo.Key, keyInfo.KeyChar, keyInfo.Modifiers);
        try
        {
            var keyPressArgs = new KeyPressEventArgs(keyInfo);

            // Check for registered key bindings first
            var binding = new KeyBinding(keyInfo.Key, keyInfo.Modifiers);
            if (_keyBindings.TryGetValue(binding, out var handler))
            {
                handler(keyPressArgs);
                if (keyPressArgs.Handled)
                {
                    return;
                }
            }

            // Raise key combination event for complex key combinations (Ctrl or Alt, but not just Shift)
            if ((keyInfo.Modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
            {
                var combinationArgs = new KeyCombinationEventArgs(keyInfo.Key, keyInfo.Modifiers, keyInfo.KeyChar);
                KeyCombinationPressed?.Invoke(this, combinationArgs);
                if (combinationArgs.Handled)
                {
                    return;
                }
            }

            // Raise character typed event for printable characters
            if (!char.IsControl(keyInfo.KeyChar) && keyInfo.KeyChar != '\0')
            {
                _logger?.LogTrace("Firing CharacterTyped event with char: '{Char}'", keyInfo.KeyChar);
                var charArgs = new CharacterTypedEventArgs(keyInfo.KeyChar);
                CharacterTyped?.Invoke(this, charArgs);
                _logger?.LogTrace("CharacterTyped event invoked.");
            }
            else
            {
                // It's a control key, so fire the general key pressed event
                KeyPressed?.Invoke(this, keyPressArgs);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing key input: {Key}", keyInfo.Key);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Registers default key bindings for common operations.
    /// </summary>
    private void RegisterDefaultKeyBindings()
    {
        // Global shortcuts - these will be handled by TuiApp
        // Ctrl+C is handled separately as it's a termination signal

        // Clear screen shortcut (Ctrl+L)
        RegisterKeyBinding(ConsoleKey.L, ConsoleModifiers.Control, args =>
        {
            _logger?.LogTrace("Ctrl+L detected - clear screen requested");
            // This will be handled by the consuming application
        });

        _logger?.LogTrace("Default key bindings registered");
    }

    /// <summary>
    /// Gets the current keyboard input statistics.
    /// </summary>
    public KeyboardStatistics GetStatistics()
    {
        return Statistics with
        {
            IsRunning = IsRunning,
            RegisteredBindingsCount = _keyBindings.Count
        };
    }

    /// <summary>
    /// Disposes the keyboard handler and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Stop the input handling
        _cancellationTokenSource.Cancel();

        // Wait for the input task to complete (with timeout)
        if (_inputTask != null && !_inputTask.IsCompleted)
        {
            try
            {
                _ = _inputTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        _cancellationTokenSource.Dispose();
        _keyBindings.Clear();

        // Clear event subscriptions
        KeyPressed = null;
        KeyCombinationPressed = null;
        CharacterTyped = null;

        _logger?.LogTrace("AdvancedKeyboardHandler disposed");

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a key binding combination.
/// </summary>
public readonly record struct KeyBinding(ConsoleKey Key, ConsoleModifiers Modifiers);

/// <summary>
/// Event arguments for key press events.
/// </summary>
/// <remarks>
/// Initializes a new instance of KeyPressEventArgs.
/// </remarks>
public sealed class KeyPressEventArgs(ConsoleKeyInfo keyInfo) : EventArgs
{
    /// <summary>
    /// Gets the console key information.
    /// </summary>
    public ConsoleKeyInfo KeyInfo { get; } = keyInfo;

    /// <summary>
    /// Gets or sets whether the key press has been handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets the key that was pressed.
    /// </summary>
    public ConsoleKey Key => KeyInfo.Key;

    /// <summary>
    /// Gets the modifiers that were pressed.
    /// </summary>
    public ConsoleModifiers Modifiers => KeyInfo.Modifiers;

    /// <summary>
    /// Gets the character representation of the key.
    /// </summary>
    public char KeyChar => KeyInfo.KeyChar;
}

/// <summary>
/// Event arguments for key combination events.
/// </summary>
/// <remarks>
/// Initializes a new instance of KeyCombinationEventArgs.
/// </remarks>
public sealed class KeyCombinationEventArgs(ConsoleKey key, ConsoleModifiers modifiers, char keyChar) : EventArgs
{
    /// <summary>
    /// Gets the key that was pressed.
    /// </summary>
    public ConsoleKey Key { get; } = key;

    /// <summary>
    /// Gets the modifiers that were pressed.
    /// </summary>
    public ConsoleModifiers Modifiers { get; } = modifiers;

    /// <summary>
    /// Gets the character representation of the key.
    /// </summary>
    public char KeyChar { get; } = keyChar;

    /// <summary>
    /// Gets or sets whether the key combination has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Event arguments for character typed events.
/// </summary>
/// <remarks>
/// Initializes a new instance of CharacterTypedEventArgs.
/// </remarks>
public sealed class CharacterTypedEventArgs(char character) : EventArgs
{
    /// <summary>
    /// Gets the character that was typed.
    /// </summary>
    public char Character { get; } = character;

    /// <summary>
    /// Gets or sets whether the character has been handled.
    /// </summary>
    public bool Handled { get; set; }
}

/// <summary>
/// Contains keyboard input statistics and performance metrics.
/// </summary>
public sealed record KeyboardStatistics(
    bool IsRunning = false,
    long TotalKeysProcessed = 0,
    int RegisteredBindingsCount = 0,
    DateTime? LastKeyPressTime = null)
{
    /// <summary>
    /// Gets the keys per second rate based on recent activity.
    /// </summary>
    public double KeysPerSecond
    {
        get
        {
            if (LastKeyPressTime == null || TotalKeysProcessed == 0)
            {
                return 0.0;
            }

            var elapsed = DateTime.UtcNow - LastKeyPressTime.Value;
            if (elapsed.TotalSeconds < 1.0)
            {
                return TotalKeysProcessed; // Recent activity
            }

            return TotalKeysProcessed / elapsed.TotalSeconds;
        }
    }

    /// <summary>
    /// Gets whether the keyboard handler is responsive.
    /// </summary>
    public bool IsResponsive => IsRunning && (LastKeyPressTime == null ||
        DateTime.UtcNow - LastKeyPressTime.Value < TimeSpan.FromSeconds(5));
}
