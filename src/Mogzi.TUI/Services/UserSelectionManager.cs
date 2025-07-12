namespace Mogzi.TUI.Services;

public class UserSelectionManager(IServiceProvider serviceProvider, InputContext inputContext, ILogger<UserSelectionManager> logger)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly InputContext _inputContext = inputContext;
    private readonly ILogger<UserSelectionManager> _logger = logger;
    private IUserSelectionProvider? _activeProvider;

    public bool IsSelectionModeActive => _activeProvider != null;

    public void DetectAndActivate(string input)
    {
        if (_activeProvider != null)
        {
            return;
        }

        var providers = _serviceProvider.GetServices<IUserSelectionProvider>();
        _activeProvider = providers.FirstOrDefault(p => p.Command.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (_activeProvider != null)
        {
            _logger.LogInformation("UserSelectionManager: Activated provider '{ProviderType}' for command '{Command}'",
                _activeProvider.GetType().Name, _activeProvider.Command);
            Console.WriteLine($"🔍 UserSelectionManager: Activated provider '{_activeProvider.GetType().Name}' for command '{_activeProvider.Command}'");
            _inputContext.State = InputState.UserSelection;
        }
    }

    public async Task UpdateSelectionsAsync()
    {
        if (_activeProvider == null)
        {
            return;
        }

        var selections = await _activeProvider.GetSelectionsAsync();

        _inputContext.CompletionItems.Clear();
        _inputContext.CompletionItems.AddRange(selections);
        _inputContext.SelectedSuggestionIndex = 0;
    }

    public async Task AcceptSelectionAsync()
    {
        if (_activeProvider == null || _inputContext.SelectedSuggestionIndex < 0 ||
            _inputContext.SelectedSuggestionIndex >= _inputContext.CompletionItems.Count)
        {
            return;
        }

        var selection = _inputContext.CompletionItems[_inputContext.SelectedSuggestionIndex];
        await _activeProvider.OnSelectionAsync(selection.Text);
        Deactivate();
    }

    public void Deactivate()
    {
        _activeProvider = null;
        _inputContext.Clear();
        _inputContext.State = InputState.Normal;
    }
}
