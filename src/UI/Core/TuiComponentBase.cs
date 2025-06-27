using UI.State;

namespace UI.Core;

/// <summary>
/// Abstract base class for TUI components providing state management and lifecycle hooks.
/// </summary>
public abstract class TuiComponentBase : ITuiComponent
{
    private readonly Dictionary<string, object> _stateValues = new();
    private readonly Dictionary<string, object[]> _effectDependencies = new();
    private bool _isMounted = false;
    private bool _isDisposed = false;

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public string ComponentId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Logger instance for this component.
    /// </summary>
    protected ILogger? Logger { get; set; }

    /// <summary>
    /// Renders the component to a Spectre.Console IRenderable.
    /// </summary>
    public abstract Task<IRenderable> RenderAsync(RenderContext context);

    /// <summary>
    /// Determines if the component should be re-rendered. Default implementation always returns true.
    /// Override this method to implement performance optimizations.
    /// </summary>
    public virtual bool ShouldUpdate(RenderContext context)
    {
        return true;
    }

    /// <summary>
    /// Called when the component is mounted to the UI tree.
    /// </summary>
    public virtual async Task OnMountAsync()
    {
        _isMounted = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when the component is unmounted from the UI tree.
    /// </summary>
    public virtual async Task OnUnmountAsync()
    {
        _isMounted = false;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates or retrieves a state value for this component.
    /// </summary>
    /// <typeparam name="T">The type of the state value.</typeparam>
    /// <param name="initialValue">The initial value if the state doesn't exist.</param>
    /// <param name="key">Optional key for the state. If not provided, uses the caller member name.</param>
    /// <returns>A TuiState instance for managing the state value.</returns>
    protected TuiState<T> UseState<T>(T initialValue, [System.Runtime.CompilerServices.CallerMemberName] string key = "")
    {
        if (!_stateValues.ContainsKey(key))
        {
            var state = new TuiState<T>(initialValue);
            state.PropertyChanged += (_, _) => OnStateChanged();
            _stateValues[key] = state;
        }

        return (TuiState<T>)_stateValues[key];
    }

    /// <summary>
    /// Registers an effect that runs when dependencies change.
    /// </summary>
    /// <param name="effect">The effect function to run.</param>
    /// <param name="dependencies">The dependency array to watch for changes.</param>
    /// <param name="key">Optional key for the effect. If not provided, uses the caller member name.</param>
    protected void UseEffect(Func<Task> effect, object[] dependencies, [System.Runtime.CompilerServices.CallerMemberName] string key = "")
    {
        if (!_effectDependencies.ContainsKey(key) || !dependencies.SequenceEqual(_effectDependencies[key]))
        {
            _effectDependencies[key] = dependencies;
            
            // Run effect asynchronously without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await effect();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error running effect in component {ComponentId}", ComponentId);
                }
            });
        }
    }

    /// <summary>
    /// Called when any state in this component changes.
    /// </summary>
    protected virtual void OnStateChanged()
    {
        // Notify the rendering system that this component needs to be re-rendered
        StateChangeNotifier.NotifyStateChanged(ComponentId);
    }

    /// <summary>
    /// Helper method to create a simple text renderable.
    /// </summary>
    protected static IRenderable CreateText(string text, Style? style = null)
    {
        return new Text(text, style ?? Style.Plain);
    }

    /// <summary>
    /// Helper method to create a panel with content.
    /// </summary>
    protected static IRenderable CreatePanel(string title, IRenderable content, Color? borderColor = null)
    {
        var panel = new Panel(content)
            .Header(title)
            .Border(BoxBorder.Rounded);

        if (borderColor.HasValue)
        {
            panel.BorderColor(borderColor.Value);
        }

        return panel;
    }

    /// <summary>
    /// Helper method to create a layout with multiple renderables.
    /// </summary>
    protected static IRenderable CreateLayout(params IRenderable[] renderables)
    {
        return new Rows(renderables);
    }

    /// <summary>
    /// Disposes the component and cleans up resources.
    /// </summary>
    public virtual void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        
        // Clean up state subscriptions
        foreach (var state in _stateValues.Values.OfType<IDisposable>())
        {
            state.Dispose();
        }
        
        _stateValues.Clear();
        _effectDependencies.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Checks if the component is currently mounted.
    /// </summary>
    protected bool IsMounted => _isMounted;

    /// <summary>
    /// Checks if the component has been disposed.
    /// </summary>
    protected bool IsDisposed => _isDisposed;
}

/// <summary>
/// Static class for notifying the rendering system of state changes.
/// </summary>
internal static class StateChangeNotifier
{
    private static readonly List<Action<string>> _listeners = new();

    public static void AddListener(Action<string> listener)
    {
        _listeners.Add(listener);
    }

    public static void RemoveListener(Action<string> listener)
    {
        _listeners.Remove(listener);
    }

    public static void NotifyStateChanged(string componentId)
    {
        foreach (var listener in _listeners)
        {
            try
            {
                listener(componentId);
            }
            catch
            {
                // Ignore listener errors to prevent cascading failures
            }
        }
    }
}
