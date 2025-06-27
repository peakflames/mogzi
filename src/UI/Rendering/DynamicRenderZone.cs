namespace UI.Rendering;

/// <summary>
/// Manages rendering of dynamic components that change frequently.
/// Optimized for real-time updates without caching.
/// </summary>
public sealed class DynamicRenderZone : IDisposable
{
    private readonly List<ITuiComponent> _components = new();
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
    /// Gets the average render time for components in this zone.
    /// </summary>
    public double AverageRenderTimeMs { get; private set; }

    /// <summary>
    /// Gets the total number of renders performed.
    /// </summary>
    public long TotalRenders { get; private set; }

    /// <summary>
    /// Initializes a new instance of DynamicRenderZone.
    /// </summary>
    /// <param name="logger">Optional logger for debugging.</param>
    public DynamicRenderZone(ILogger? logger = null)
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
                _logger?.LogTrace("Registered dynamic component {ComponentId}", component.ComponentId);
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
                _lastRenderTime.Remove(component.ComponentId);
                _logger?.LogTrace("Unregistered dynamic component {ComponentId}", component.ComponentId);
            }
        }
    }

    /// <summary>
    /// Renders all components in this zone. Always renders fresh without caching.
    /// </summary>
    /// <param name="context">The render context.</param>
    /// <returns>A combined renderable for all components in this zone.</returns>
    public async Task<IRenderable> RenderAsync(RenderContext context)
    {
        if (_isDisposed) return new Text("Dynamic zone disposed");

        var renderStart = System.Diagnostics.Stopwatch.StartNew();
        var renderables = new List<IRenderable>();
        var componentsToRender = new List<ITuiComponent>();

        lock (_lock)
        {
            componentsToRender.AddRange(_components);
        }

        var renderTasks = componentsToRender.Select(async component =>
        {
            try
            {
                var componentRenderStart = System.Diagnostics.Stopwatch.StartNew();
                
                // Always render dynamic components fresh
                var renderable = await component.RenderAsync(context);
                
                var renderTime = componentRenderStart.Elapsed.TotalMilliseconds;
                
                lock (_lock)
                {
                    _lastRenderTime[component.ComponentId] = DateTime.UtcNow;
                }

                _logger?.LogTrace("Rendered dynamic component {ComponentId} in {RenderTime:F2}ms", 
                    component.ComponentId, renderTime);

                return renderable;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rendering dynamic component {ComponentId}", component.ComponentId);
                
                // Return error placeholder
                return new Panel(new Text($"Error rendering component: {ex.Message}"))
                    .Header($"Component Error ({component.ComponentId})")
                    .BorderColor(Color.Red);
            }
        });

        var results = await Task.WhenAll(renderTasks);
        renderables.AddRange(results.Where(r => r != null));

        // Update performance metrics
        var totalRenderTime = renderStart.Elapsed.TotalMilliseconds;
        TotalRenders++;
        AverageRenderTimeMs = (AverageRenderTimeMs * (TotalRenders - 1) + totalRenderTime) / TotalRenders;

        _logger?.LogTrace("Rendered {Count} dynamic components in {TotalTime:F2}ms", 
            renderables.Count, totalRenderTime);

        return renderables.Count switch
        {
            0 => new Text(""),
            1 => renderables[0],
            _ => new Rows(renderables)
        };
    }

    /// <summary>
    /// Forces all components to re-render on the next render cycle.
    /// </summary>
    public void InvalidateAll()
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            _lastRenderTime.Clear();
            _logger?.LogDebug("Invalidated all dynamic components");
        }
    }

    /// <summary>
    /// Forces a specific component to re-render on the next render cycle.
    /// </summary>
    /// <param name="componentId">The ID of the component to invalidate.</param>
    public void Invalidate(string componentId)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            _lastRenderTime.Remove(componentId);
            _logger?.LogTrace("Invalidated dynamic component {ComponentId}", componentId);
        }
    }

    /// <summary>
    /// Gets the last render time for a specific component.
    /// </summary>
    /// <param name="componentId">The component ID to check.</param>
    /// <returns>The last render time, or null if the component hasn't been rendered.</returns>
    public DateTime? GetLastRenderTime(string componentId)
    {
        lock (_lock)
        {
            return _lastRenderTime.TryGetValue(componentId, out var time) ? time : null;
        }
    }

    /// <summary>
    /// Gets components that haven't been rendered recently.
    /// </summary>
    /// <param name="threshold">The time threshold for considering a component stale.</param>
    /// <returns>A list of component IDs that are considered stale.</returns>
    public IReadOnlyList<string> GetStaleComponents(TimeSpan threshold)
    {
        var cutoff = DateTime.UtcNow - threshold;
        var staleComponents = new List<string>();

        lock (_lock)
        {
            foreach (var component in _components)
            {
                if (!_lastRenderTime.TryGetValue(component.ComponentId, out var lastRender) ||
                    lastRender < cutoff)
                {
                    staleComponents.Add(component.ComponentId);
                }
            }
        }

        return staleComponents;
    }

    /// <summary>
    /// Gets statistics about the dynamic render zone.
    /// </summary>
    /// <returns>A dictionary containing zone statistics.</returns>
    public IReadOnlyDictionary<string, object> GetStatistics()
    {
        lock (_lock)
        {
            var recentlyRendered = _lastRenderTime.Values
                .Count(time => DateTime.UtcNow - time < TimeSpan.FromSeconds(1));

            return new Dictionary<string, object>
            {
                ["ComponentCount"] = _components.Count,
                ["TotalRenders"] = TotalRenders,
                ["AverageRenderTimeMs"] = AverageRenderTimeMs,
                ["RecentlyRenderedCount"] = recentlyRendered,
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
    /// Gets performance metrics for individual components.
    /// </summary>
    /// <returns>A dictionary mapping component IDs to their last render times.</returns>
    public IReadOnlyDictionary<string, DateTime> GetComponentMetrics()
    {
        lock (_lock)
        {
            return new Dictionary<string, DateTime>(_lastRenderTime);
        }
    }

    /// <summary>
    /// Resets performance metrics.
    /// </summary>
    public void ResetMetrics()
    {
        if (_isDisposed) return;

        TotalRenders = 0;
        AverageRenderTimeMs = 0;
        
        lock (_lock)
        {
            _lastRenderTime.Clear();
        }

        _logger?.LogDebug("Reset dynamic zone metrics");
    }

    /// <summary>
    /// Disposes the dynamic render zone and cleans up resources.
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
                    _logger?.LogWarning(ex, "Error disposing dynamic component {ComponentId}", component.ComponentId);
                }
            }

            _components.Clear();
            _lastRenderTime.Clear();
        }

        _logger?.LogDebug("DynamicRenderZone disposed");

        GC.SuppressFinalize(this);
    }
}
