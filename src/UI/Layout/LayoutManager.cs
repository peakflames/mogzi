namespace UI.Layout;

/// <summary>
/// Manages layout calculations and height distribution for the TUI application.
/// </summary>
public sealed class LayoutManager
{
    private readonly ILogger<LayoutManager>? _logger;
    private LayoutConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of LayoutManager.
    /// </summary>
    /// <param name="configuration">The layout configuration to use.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    public LayoutManager(LayoutConfiguration? configuration = null, ILogger<LayoutManager>? logger = null)
    {
        _configuration = configuration ?? LayoutConfiguration.Default;
        _logger = logger;
    }

    /// <summary>
    /// Calculates the layout for the given terminal size.
    /// </summary>
    /// <param name="terminalSize">The current terminal size.</param>
    /// <returns>A layout result containing constraints for each zone.</returns>
    public LayoutResult CalculateLayout(TerminalSize terminalSize)
    {
        _logger?.LogTrace("Calculating layout for terminal size {Width}x{Height}", 
            terminalSize.Width, terminalSize.Height);

        // Ensure minimum terminal size
        var effectiveWidth = Math.Max(terminalSize.Width, _configuration.MinimumWidth);
        var effectiveHeight = Math.Max(terminalSize.Height, _configuration.MinimumHeight);

        // Calculate available height after accounting for margins
        var availableHeight = effectiveHeight - _configuration.TopMargin - _configuration.BottomMargin;
        var availableWidth = effectiveWidth - _configuration.LeftMargin - _configuration.RightMargin;

        // Distribute height between static and dynamic zones
        var (staticHeight, dynamicHeight) = DistributeHeight(availableHeight);

        // Create constraints for each zone
        var staticConstraints = new LayoutConstraints(
            staticHeight,
            availableWidth,
            _configuration.LeftMargin,
            _configuration.TopMargin
        );

        var dynamicConstraints = new LayoutConstraints(
            dynamicHeight,
            availableWidth,
            _configuration.LeftMargin,
            _configuration.TopMargin + staticHeight
        );

        var result = new LayoutResult(
            terminalSize,
            staticConstraints,
            dynamicConstraints,
            availableWidth,
            availableHeight
        );

        _logger?.LogTrace("Layout calculated: Static={StaticHeight}h, Dynamic={DynamicHeight}h", 
            staticHeight, dynamicHeight);

        return result;
    }

    /// <summary>
    /// Distributes the available height between static and dynamic zones.
    /// </summary>
    private (int staticHeight, int dynamicHeight) DistributeHeight(int availableHeight)
    {
        // Ensure minimum heights
        var minStaticHeight = Math.Max(1, _configuration.MinimumStaticHeight);
        var minDynamicHeight = Math.Max(1, _configuration.MinimumDynamicHeight);

        // Check if we have enough space for minimums
        if (availableHeight < minStaticHeight + minDynamicHeight)
        {
            // Not enough space, distribute proportionally
            var minStaticRatio = (double)minStaticHeight / (minStaticHeight + minDynamicHeight);
            var staticHeight = Math.Max(1, (int)(availableHeight * minStaticRatio));
            var dynamicHeight = Math.Max(1, availableHeight - staticHeight);
            
            return (staticHeight, dynamicHeight);
        }

        // Calculate preferred heights based on ratios
        var totalRatio = _configuration.StaticZoneRatio + _configuration.DynamicZoneRatio;
        var staticRatio = _configuration.StaticZoneRatio / totalRatio;
        var dynamicRatio = _configuration.DynamicZoneRatio / totalRatio;

        var preferredStaticHeight = (int)(availableHeight * staticRatio);
        var preferredDynamicHeight = (int)(availableHeight * dynamicRatio);

        // Apply constraints
        var finalStaticHeight = Math.Max(minStaticHeight, 
            Math.Min(_configuration.MaximumStaticHeight, preferredStaticHeight));
        
        var remainingHeight = availableHeight - finalStaticHeight;
        var finalDynamicHeight = Math.Max(minDynamicHeight, 
            Math.Min(_configuration.MaximumDynamicHeight, remainingHeight));

        // Adjust if dynamic height constraints affect the total
        if (finalStaticHeight + finalDynamicHeight != availableHeight)
        {
            var difference = availableHeight - (finalStaticHeight + finalDynamicHeight);
            
            // Try to give extra space to dynamic zone first
            if (difference > 0 && finalDynamicHeight < _configuration.MaximumDynamicHeight)
            {
                var dynamicIncrease = Math.Min(difference, _configuration.MaximumDynamicHeight - finalDynamicHeight);
                finalDynamicHeight += dynamicIncrease;
                difference -= dynamicIncrease;
            }
            
            // Give remaining space to static zone
            if (difference > 0 && finalStaticHeight < _configuration.MaximumStaticHeight)
            {
                finalStaticHeight += Math.Min(difference, _configuration.MaximumStaticHeight - finalStaticHeight);
            }
        }

        return (finalStaticHeight, finalDynamicHeight);
    }

    /// <summary>
    /// Updates the layout configuration.
    /// </summary>
    /// <param name="configuration">The new configuration to use.</param>
    public void UpdateConfiguration(LayoutConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger?.LogDebug("Layout configuration updated");
    }

    /// <summary>
    /// Gets the current layout configuration.
    /// </summary>
    public LayoutConfiguration GetConfiguration() => _configuration;

    /// <summary>
    /// Calculates the optimal layout for a specific use case.
    /// </summary>
    /// <param name="terminalSize">The terminal size.</param>
    /// <param name="useCase">The specific use case to optimize for.</param>
    /// <returns>A layout result optimized for the use case.</returns>
    public LayoutResult CalculateOptimalLayout(TerminalSize terminalSize, LayoutUseCase useCase)
    {
        var originalConfig = _configuration;
        
        try
        {
            // Temporarily adjust configuration based on use case
            _configuration = useCase switch
            {
                LayoutUseCase.ChatFocused => _configuration with 
                { 
                    StaticZoneRatio = 0.7, 
                    DynamicZoneRatio = 0.3 
                },
                LayoutUseCase.ToolExecutionFocused => _configuration with 
                { 
                    StaticZoneRatio = 0.3, 
                    DynamicZoneRatio = 0.7 
                },
                LayoutUseCase.Balanced => _configuration with 
                { 
                    StaticZoneRatio = 0.5, 
                    DynamicZoneRatio = 0.5 
                },
                LayoutUseCase.Compact => _configuration with 
                { 
                    MinimumStaticHeight = 5,
                    MinimumDynamicHeight = 3,
                    TopMargin = 0,
                    BottomMargin = 0
                },
                _ => _configuration
            };

            return CalculateLayout(terminalSize);
        }
        finally
        {
            _configuration = originalConfig;
        }
    }
}

/// <summary>
/// Configuration for layout calculations.
/// </summary>
public sealed record LayoutConfiguration(
    double StaticZoneRatio = 0.6,
    double DynamicZoneRatio = 0.4,
    int MinimumStaticHeight = 10,
    int MaximumStaticHeight = int.MaxValue,
    int MinimumDynamicHeight = 5,
    int MaximumDynamicHeight = int.MaxValue,
    int MinimumWidth = 60,
    int MinimumHeight = 20,
    int TopMargin = 1,
    int BottomMargin = 1,
    int LeftMargin = 1,
    int RightMargin = 1)
{
    /// <summary>
    /// Gets the default layout configuration.
    /// </summary>
    public static LayoutConfiguration Default => new();

    /// <summary>
    /// Gets a compact layout configuration for smaller terminals.
    /// </summary>
    public static LayoutConfiguration Compact => new(
        StaticZoneRatio: 0.7,
        DynamicZoneRatio: 0.3,
        MinimumStaticHeight: 5,
        MinimumDynamicHeight: 3,
        MinimumWidth: 40,
        MinimumHeight: 15,
        TopMargin: 0,
        BottomMargin: 0,
        LeftMargin: 0,
        RightMargin: 0
    );

    /// <summary>
    /// Gets a spacious layout configuration for larger terminals.
    /// </summary>
    public static LayoutConfiguration Spacious => new(
        StaticZoneRatio: 0.5,
        DynamicZoneRatio: 0.5,
        MinimumStaticHeight: 15,
        MinimumDynamicHeight: 10,
        MinimumWidth: 80,
        MinimumHeight: 30,
        TopMargin: 2,
        BottomMargin: 2,
        LeftMargin: 2,
        RightMargin: 2
    );

    /// <summary>
    /// Validates the configuration values.
    /// </summary>
    public bool IsValid =>
        StaticZoneRatio > 0 &&
        DynamicZoneRatio > 0 &&
        MinimumStaticHeight > 0 &&
        MinimumDynamicHeight > 0 &&
        MinimumWidth > 0 &&
        MinimumHeight > 0 &&
        MaximumStaticHeight >= MinimumStaticHeight &&
        MaximumDynamicHeight >= MinimumDynamicHeight;
}

/// <summary>
/// Result of a layout calculation.
/// </summary>
public sealed record LayoutResult(
    TerminalSize TerminalSize,
    LayoutConstraints StaticZoneConstraints,
    LayoutConstraints DynamicZoneConstraints,
    int AvailableWidth,
    int AvailableHeight)
{
    /// <summary>
    /// Gets the total height used by both zones.
    /// </summary>
    public int TotalUsedHeight => StaticZoneConstraints.Height + DynamicZoneConstraints.Height;

    /// <summary>
    /// Gets the efficiency of space usage (0.0 to 1.0).
    /// </summary>
    public double SpaceEfficiency => (double)TotalUsedHeight / TerminalSize.Height;

    /// <summary>
    /// Gets whether the layout is considered optimal.
    /// </summary>
    public bool IsOptimal => SpaceEfficiency > 0.8 && 
                            StaticZoneConstraints.Height >= 5 && 
                            DynamicZoneConstraints.Height >= 3;
}

/// <summary>
/// Enumeration of layout use cases for optimization.
/// </summary>
public enum LayoutUseCase
{
    /// <summary>
    /// Balanced layout suitable for general use.
    /// </summary>
    Balanced,

    /// <summary>
    /// Layout optimized for chat/conversation viewing.
    /// </summary>
    ChatFocused,

    /// <summary>
    /// Layout optimized for tool execution monitoring.
    /// </summary>
    ToolExecutionFocused,

    /// <summary>
    /// Compact layout for smaller terminals.
    /// </summary>
    Compact
}
