namespace Mogzi.TUI.Components;

/// <summary>
/// Manages the lifecycle and coordination of TUI components.
/// Handles component registration, layout composition, and input distribution.
/// </summary>
public class TuiComponentManager(ILogger<TuiComponentManager> logger) : ITuiComponentManager
{
    private readonly Dictionary<string, ITuiComponent> _components = [];
    private readonly ILogger<TuiComponentManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IReadOnlyDictionary<string, ITuiComponent> Components => _components.AsReadOnly();
    public ITuiLayout? CurrentLayout { get; set; }

    public void RegisterComponent(ITuiComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_components.ContainsKey(component.Name))
        {
            _logger.LogWarning("Component {ComponentName} is already registered, replacing existing component", component.Name);
        }

        _components[component.Name] = component;
    }

    public bool UnregisterComponent(string componentName)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentName);

        if (_components.Remove(componentName))
        {
            return true;
        }

        _logger.LogWarning("Attempted to unregister non-existent component: {ComponentName}", componentName);
        return false;
    }

    public ITuiComponent? GetComponent(string componentName)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentName);
        return _components.GetValueOrDefault(componentName);
    }

    public T? GetComponent<T>(string componentName) where T : class, ITuiComponent
    {
        return GetComponent(componentName) as T;
    }

    public IRenderable RenderLayout(IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);


        if (CurrentLayout == null)
        {
            _logger.LogWarning("No layout set, returning empty content");
            return new Text("No layout configured");
        }

        if (!CurrentLayout.ValidateComponents(_components))
        {
            var requiredComponents = CurrentLayout.GetRequiredComponents();
            var missingComponents = requiredComponents.Where(name => !_components.ContainsKey(name));
            _logger.LogError("Layout validation failed. Missing components: {MissingComponents}", string.Join(", ", missingComponents));
            return new Text($"Layout error: Missing components: {string.Join(", ", missingComponents)}");
        }

        try
        {
            var result = CurrentLayout.Compose(_components, context);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering layout: {LayoutName}", CurrentLayout.Name);
            return new Text($"Layout rendering error: {ex.Message}");
        }
    }

    public async Task<bool> BroadcastInputAsync(object inputEvent, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        ArgumentNullException.ThrowIfNull(context);

        var handled = false;

        foreach (var component in _components.Values.Where(c => c.IsVisible))
        {
            try
            {
                if (await component.HandleInputAsync(context, inputEvent))
                {
                    handled = true;
                    break; // Stop at first component that handles the input
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling input in component: {ComponentName}", component.Name);
            }
        }

        return handled;
    }

    public async Task InitializeComponentsAsync(IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);


        foreach (var component in _components.Values)
        {
            try
            {
                await component.InitializeAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing component: {ComponentName}", component.Name);
            }
        }

    }

    public async Task DisposeComponentsAsync()
    {

        foreach (var component in _components.Values)
        {
            try
            {
                await component.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing component: {ComponentName}", component.Name);
            }
        }

        _components.Clear();
    }

    public void SetComponentVisibility(string componentName, bool isVisible)
    {
        ArgumentException.ThrowIfNullOrEmpty(componentName);

        if (_components.TryGetValue(componentName, out var component))
        {
            component.IsVisible = isVisible;
        }
        else
        {
            _logger.LogWarning("Attempted to set visibility for non-existent component: {ComponentName}", componentName);
        }
    }

    public void UpdateComponentVisibility(ChatState currentState, IRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Update component visibility based on current state
        switch (currentState)
        {
            case ChatState.Input:
                SetComponentVisibility("InputPanel", true);
                SetComponentVisibility("FooterPanel", true);
                SetComponentVisibility("ProgressPanel", false);

                // Show autocomplete/user selection panels based on input context
                var inputContext = context.TuiContext.InputContext;
                SetComponentVisibility("AutocompletePanel", inputContext.State == InputState.Autocomplete && inputContext.ShowSuggestions);
                SetComponentVisibility("UserSelectionPanel", inputContext.State == InputState.UserSelection);

                // Welcome panel is now rendered to static area in FlexColumnTuiApp.RenderInitialContent()
                // so we don't show it in dynamic area to prevent duplication
                SetComponentVisibility("WelcomePanel", false);
                break;

            case ChatState.Thinking:
            case ChatState.ToolExecution:
                SetComponentVisibility("InputPanel", false);
                SetComponentVisibility("AutocompletePanel", false);
                SetComponentVisibility("UserSelectionPanel", false);
                SetComponentVisibility("WelcomePanel", false);
                SetComponentVisibility("ProgressPanel", true);
                SetComponentVisibility("FooterPanel", true);
                break;

            default:
                _logger.LogWarning("Unknown chat state: {CurrentState}", currentState);
                break;
        }

    }
}
