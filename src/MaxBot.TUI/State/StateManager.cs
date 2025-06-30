namespace MaxBot.TUI.State;

/// <summary>
/// Manages global state changes and coordinates re-rendering across the application.
/// </summary>
public sealed class StateManager : IDisposable
{
    private readonly HashSet<string> _pendingChanges = [];
    private readonly Dictionary<string, DateTime> _lastChangeTime = [];
    private readonly Lock _lock = new();
    private readonly Timer _debounceTimer;
    private readonly ILogger<StateManager>? _logger;
    private bool _isDisposed = false;

    /// <summary>
    /// Event raised when state changes are ready to be processed.
    /// </summary>
    public event Action? StateChangesReady;

    /// <summary>
    /// Gets whether there are pending state changes that need to be processed.
    /// </summary>
    public bool HasPendingChanges
    {
        get
        {
            lock (_lock)
            {
                return _pendingChanges.Count > 0;
            }
        }
    }

    /// <summary>
    /// Gets the number of pending state changes.
    /// </summary>
    public int PendingChangesCount
    {
        get
        {
            lock (_lock)
            {
                return _pendingChanges.Count;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of StateManager.
    /// </summary>
    /// <param name="logger">Optional logger for debugging state changes.</param>
    public StateManager(ILogger<StateManager>? logger = null)
    {
        _logger = logger;
        _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

        // Subscribe to component state changes
        // Core.StateChangeNotifier.AddListener(NotifyStateChanged);
    }

    /// <summary>
    /// Notifies the state manager that a component's state has changed.
    /// </summary>
    /// <param name="componentId">The ID of the component that changed.</param>
    public void NotifyStateChanged(string componentId)
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            _ = _pendingChanges.Add(componentId);
            _lastChangeTime[componentId] = DateTime.UtcNow;

            _logger?.LogTrace("State changed for component {ComponentId}", componentId);
        }

        // Debounce the state change notifications to batch updates
        _ = _debounceTimer.Change(16, Timeout.Infinite);
    }

    /// <summary>
    /// Forces immediate processing of all pending state changes.
    /// </summary>
    public void FlushPendingChanges()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            if (_pendingChanges.Count > 0)
            {
                _logger?.LogDebug("Flushing {Count} pending state changes", _pendingChanges.Count);
                StateChangesReady?.Invoke();
            }
        }
    }

    /// <summary>
    /// Clears all pending state changes after they have been processed.
    /// </summary>
    public void ClearPendingChanges()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            var clearedCount = _pendingChanges.Count;
            _pendingChanges.Clear();

            if (clearedCount > 0)
            {
                _logger?.LogTrace("Cleared {Count} processed state changes", clearedCount);
            }
        }
    }

    /// <summary>
    /// Gets a snapshot of the current pending changes.
    /// </summary>
    /// <returns>A read-only collection of component IDs with pending changes.</returns>
    public IReadOnlySet<string> GetPendingChanges()
    {
        lock (_lock)
        {
            return new HashSet<string>(_pendingChanges);
        }
    }

    /// <summary>
    /// Gets the last change time for a specific component.
    /// </summary>
    /// <param name="componentId">The component ID to check.</param>
    /// <returns>The last change time, or null if the component has no recorded changes.</returns>
    public DateTime? GetLastChangeTime(string componentId)
    {
        lock (_lock)
        {
            return _lastChangeTime.TryGetValue(componentId, out var time) ? time : null;
        }
    }

    /// <summary>
    /// Checks if a specific component has pending changes.
    /// </summary>
    /// <param name="componentId">The component ID to check.</param>
    /// <returns>True if the component has pending changes, false otherwise.</returns>
    public bool HasPendingChangesFor(string componentId)
    {
        lock (_lock)
        {
            return _pendingChanges.Contains(componentId);
        }
    }

    /// <summary>
    /// Resets the state manager, clearing all pending changes and history.
    /// </summary>
    public void Reset()
    {
        if (_isDisposed)
        {
            return;
        }

        lock (_lock)
        {
            _pendingChanges.Clear();
            _lastChangeTime.Clear();
            _logger?.LogDebug("State manager reset");
        }
    }

    /// <summary>
    /// Gets statistics about state management performance.
    /// </summary>
    /// <returns>A dictionary containing performance statistics.</returns>
    public IReadOnlyDictionary<string, object> GetStatistics()
    {
        lock (_lock)
        {
            return new Dictionary<string, object>
            {
                ["PendingChanges"] = _pendingChanges.Count,
                ["TrackedComponents"] = _lastChangeTime.Count,
                ["IsDisposed"] = _isDisposed
            };
        }
    }

    /// <summary>
    /// Called when the debounce timer elapses to process batched state changes.
    /// </summary>
    private void OnDebounceTimerElapsed(object? state)
    {
        if (_isDisposed)
        {
            return;
        }

        try
        {
            FlushPendingChanges();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing batched state changes");
        }
    }

    /// <summary>
    /// Disposes the state manager and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Unsubscribe from state change notifications
        // Core.StateChangeNotifier.RemoveListener(NotifyStateChanged);

        // Dispose the timer
        _debounceTimer?.Dispose();

        // Clear all state
        lock (_lock)
        {
            _pendingChanges.Clear();
            _lastChangeTime.Clear();
        }

        // Clear event subscriptions
        StateChangesReady = null;

        _logger?.LogDebug("State manager disposed");

        GC.SuppressFinalize(this);
    }
}
