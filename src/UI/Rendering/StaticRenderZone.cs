namespace UI.Rendering;

/// <summary>
/// Manages rendering of static components that change infrequently.
/// Implements caching to improve performance.
/// </summary>
public sealed class StaticRenderZone : IDisposable
{
    private readonly List<ITuiComponent> _components = new();
    private readonly Dictionary<string, IRenderable> _cachedRenderables = new();
    private readonly Dictionary<string, DateTime> _lastRenderTime = new();
    private readonly ILogger? _logger;
    private readonly object _lock = new();
    private bool _isDisposed = false;

    /// <summary>
    /// Gets the number of components in this zone.
    /// </summary>
    public int ComponentCount
    {
        get
        {
            lock (_lock)
            {
                return _components.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of cached renderables.
    /// </summary>
    public int CachedRenderableCount
    {
        get
        {
            lock (_lock)
            {
                return _cachedRenderables.Count;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of StaticRenderZone.
    /// </summary>
    /// <param name="logger">Optional logger for debugging.</param>
    public StaticRenderZone(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a component with this render zone.
    /// </summary>
    /// <param name="component">The component to register.</param>
    public void RegisterComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        if (component == null) throw new ArgumentNullException(nameof(component));

        lock (_lock)
        {
            if (!_components.Contains(component))
            {
                _components.Add(component);
                _logger?.LogTrace("Registered static component {ComponentId}", component.ComponentId);
            }
        }
    }

    /// <summary>
    /// Unregisters a component from this render zone.
    /// </summary>
    /// <param name="component">The component to unregister.</param>
    public void UnregisterComponent(ITuiComponent component)
    {
        if (_isDisposed) return;
        if (component == null) return;

        lock (_lock)
        {
            if (_components.Remove(component))
            {
                // Remove cached renderable
                _cachedRenderables.Remove(component.ComponentId);
                _lastRenderTime.Remove(component.ComponentId);
                _logger?.LogTrace("Unregistered static component {ComponentId}", component.ComponentId);
            }
        }
    }

    /// <summary>
    /// Renders all components in this zone, using caching for performance.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <returns>A combined renderable for all components in this zone.</returns>
    public async Task<IRenderable> RenderAsync(RenderContext context)
    {
        if (_isDisposed) return new Text("Static zone disposed");

        var renderables = new List<IRenderable>();
        var componentsToRender = new List<ITuiComponent>();

        lock (_lock)
        {
            componentsToRender.AddRange(_components);
        }

        foreach (var component in componentsToRender)
        {
            try
            {
                var renderable = await GetOrRenderComponentAsync(component, context);
                if (renderable != null)
                {
                    renderables.Add(renderable);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rendering static component {ComponentId}", component.ComponentId);
                
                // Add error placeholder
                renderables.Add(new Panel(new Text($"Error rendering component: {ex.Message}"))
                    .Header($"Component Error ({component.ComponentId})")
                    .BorderColor(Color.Red));
            }
        }

        return renderables.Count switch
        {
            0 => new Text(""),
            1 => renderables[0],
            _ => new Rows(renderables)
        };
    }

    /// <summary>
    /// Gets a cached renderable or renders the component if needed.
    /// </summary>
    private async Task<IRenderable?> GetOrRenderComponentAsync(ITuiComponent component, RenderContext context)
    {
        var componentId = component.ComponentId;
        var shouldRender = context.ForceRender || ShouldRenderComponent(component, context);

        if (shouldRender)
        {
            var renderable = await component.RenderAsync(context);
            
            lock (_lock)
            {
                _cachedRenderables[componentId] = renderable;
                _lastRenderTime[componentId] = DateTime.UtcNow;
            }

            _logger?.LogTrace("Rendered and cached static component {ComponentId}", componentId);
            return renderable;
        }

        // Return cached renderable if available
        lock (_lock)
        {
            if (_cachedRenderables.TryGetValue(componentId, out var cached))
            {
                _logger?.LogTrace("Using cached renderable for static component {ComponentId}", componentId);
                return cached;
            }
        }

        // No cache available, render once
        var newRenderable = await component.RenderAsync(context);
        
        lock (_lock)
        {
            _cachedRenderables[componentId] = newRenderable;
            _lastRenderTime[componentId] = DateTime.UtcNow;
        }

        _logger?.LogTrace("Initial render and cache for static component {ComponentId}", componentId);
        return newRenderable;
    }

    /// <summary>
    /// Determines if a component should be re-rendered.
    /// </summary>
    private bool ShouldRenderComponent(ITuiComponent component, RenderContext context)
    {
        // Always render if component says it should update
        if (component.ShouldUpdate(context))
        {
            return true;
        }

        // Check if component has been rendered recently
        lock (_lock)
        {
            if (_lastRenderTime.TryGetValue(component.ComponentId, out var lastRender))
            {
                // Re-render if it's been more than 5 minutes (for very static content)
                return DateTime.UtcNow - lastRender > TimeSpan.FromMinutes(5);
            }
        }

        // Component has never been rendered
        return true;
    }

    /// <summary>
    /// Clears all cached renderables, forcing a re-render of all components.
    /// </summary>
    public void ClearCache()
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var clearedCount = _cachedRenderables.Count;
            _cachedRenderables.Clear();
            _lastRenderTime.Clear();
            
            _logger?.LogDebug("Cleared {Count} cached renderables from static zone", clearedCount);
        }
    }

    /// <summary>
    /// Clears the cache for a specific component.
    /// </summary>
    /// <param name="componentId">The ID of the component to clear from cache.</param>
    public void ClearCache(string componentId)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            if (_cachedRenderables.Remove(componentId))
            {
                _lastRenderTime.Remove(componentId);
                _logger?.LogTrace("Cleared cache for static component {ComponentId}", componentId);
            }
        }
    }

    /// <summary>
    /// Gets statistics about the static render zone.
    /// </summary>
    /// <returns>A dictionary containing zone statistics.</returns>
    public IReadOnlyDictionary<string, object> GetStatistics()
    {
        lock (_lock)
        {
            return new Dictionary<string, object>
            {
                ["ComponentCount"] = _components.Count,
                ["CachedRenderableCount"] = _cachedRenderables.Count,
                ["CacheHitRatio"] = _cachedRenderables.Count > 0 ? 
                    (double)_cachedRenderables.Count / _components.Count : 0.0,
                ["IsDisposed"] = _isDisposed
            };
        }
    }

    /// <summary>
    /// Gets the component IDs that are currently registered.
    /// </summary>
    /// <returns>A read-only collection of component IDs.</returns>
    public IReadOnlyList<string> GetComponentIds()
    {
        lock (_lock)
        {
            return _components.Select(c => c.ComponentId).ToList();
        }
    }

    /// <summary>
    /// Disposes the static render zone and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;

        lock (_lock)
        {
            // Dispose all components
            foreach (var component in _components)
            {
                try
                {
                    component.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Error disposing static component {ComponentId}", component.ComponentId);
                }
            }

            _components.Clear();
            _cachedRenderables.Clear();
            _lastRenderTime.Clear();
        }

        _logger?.LogDebug("StaticRenderZone disposed");

        GC.SuppressFinalize(this);
    }
}
