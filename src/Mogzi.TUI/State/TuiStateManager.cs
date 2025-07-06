
namespace Mogzi.TUI.State;

/// <summary>
/// Concrete implementation of ITuiStateManager that manages the state machine for the TUI application.
/// </summary>
public class TuiStateManager(ILogger<TuiStateManager> logger) : ITuiStateManager
{
    private readonly ILogger<TuiStateManager> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Dictionary<ChatState, Func<ITuiState>> _stateFactories = [];
    private readonly Lock _stateLock = new();

    private ITuiContext? _context;
    private ITuiState? _currentState;
    private ChatState _currentStateType = ChatState.Input;

    public ITuiState? CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public ChatState CurrentStateType
    {
        get
        {
            lock (_stateLock)
            {
                return _currentStateType;
            }
        }
    }

    public Task InitializeAsync(ITuiContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger.LogTrace("TuiStateManager initialized with context");

        // Start with Input state
        return TransitionToStateAsync(ChatState.Input);
    }

    public async Task TransitionToStateAsync(ChatState newStateType)
    {
        if (_context == null)
        {
            throw new InvalidOperationException("TuiStateManager must be initialized before transitioning states");
        }

        ITuiState? previousState;
        ITuiState? newState;

        lock (_stateLock)
        {
            if (_currentStateType == newStateType && _currentState != null)
            {
                _logger.LogTrace("Already in state {StateType}, skipping transition", newStateType);
                return;
            }

            previousState = _currentState;

            // Create new state instance
            if (!_stateFactories.TryGetValue(newStateType, out var stateFactory))
            {
                throw new InvalidOperationException($"No state factory registered for state type: {newStateType}");
            }

            newState = stateFactory();
            _currentState = newState;
            _currentStateType = newStateType;
        }

        try
        {
            _logger.LogTrace("Transitioning from {PreviousState} to {NewState}",
                previousState?.Name ?? "none", newState.Name);

            // Call exit on previous state
            if (previousState != null)
            {
                await previousState.OnExitAsync(_context, newState);
            }

            // Call enter on new state
            await newState.OnEnterAsync(_context, previousState);

            _logger.LogTrace("Successfully transitioned to {NewState}", newState.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during state transition from {PreviousState} to {NewState}",
                previousState?.Name ?? "none", newState.Name);
            throw;
        }
    }

    public IRenderable RenderDynamicContent()
    {
        if (_context == null || _currentState == null)
        {
            return new Text("State manager not initialized");
        }

        try
        {
            return _currentState.RenderDynamicContent(_context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rendering dynamic content for state {StateName}", _currentState.Name);
            return new Text($"Error rendering {_currentState.Name} state: {ex.Message}");
        }
    }

    public async Task HandleKeyPressAsync(KeyPressEventArgs e)
    {
        if (_context == null || _currentState == null)
        {
            return;
        }

        try
        {
            await _currentState.HandleKeyPressAsync(_context, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling key press in state {StateName}: {Key}",
                _currentState.Name, e.Key);
        }
    }

    public async Task HandleCharacterTypedAsync(CharacterTypedEventArgs e)
    {
        _logger.LogTrace("TuiStateManager.HandleCharacterTypedAsync called with character: '{Character}'", e.Character);
        _logger.LogTrace("TuiStateManager context null check: {IsNull}", _context == null);
        _logger.LogTrace("TuiStateManager currentState null check: {IsNull}", _currentState == null);

        if (_context == null || _currentState == null)
        {
            _logger.LogWarning("TuiStateManager early return - context null: {ContextNull}, currentState null: {StateNull}",
                _context == null, _currentState == null);
            return;
        }

        try
        {
            _logger.LogTrace("TuiStateManager delegating character '{Character}' to state: {StateName}", e.Character, _currentState.Name);
            await _currentState.HandleCharacterTypedAsync(_context, e);
            _logger.LogTrace("TuiStateManager successfully delegated character '{Character}' to state: {StateName}", e.Character, _currentState.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling character typed in state {StateName}: {Character}",
                _currentState.Name, e.Character);
        }
    }

    public void RegisterState(ChatState stateType, Func<ITuiState> stateFactory)
    {
        ArgumentNullException.ThrowIfNull(stateFactory);

        lock (_stateLock)
        {
            _stateFactories[stateType] = stateFactory;
        }

        _logger.LogTrace("Registered state factory for {StateType}", stateType);
    }

    public async Task ShutdownAsync()
    {
        ITuiState? currentState;

        lock (_stateLock)
        {
            currentState = _currentState;
            _currentState = null;
        }

        if (currentState != null && _context != null)
        {
            try
            {
                await currentState.OnExitAsync(_context, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during state shutdown for {StateName}", currentState.Name);
            }
        }

        _logger.LogTrace("TuiStateManager shutdown complete");
    }
}
