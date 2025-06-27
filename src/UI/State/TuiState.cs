namespace UI.State;

/// <summary>
/// Represents a reactive state value that notifies when changed.
/// </summary>
/// <typeparam name="T">The type of the state value.</typeparam>
public sealed class TuiState<T> : INotifyPropertyChanged, IDisposable
{
    private T _value;
    private bool _isDisposed = false;

    /// <summary>
    /// Initializes a new instance of TuiState with the specified initial value.
    /// </summary>
    /// <param name="initialValue">The initial value for the state.</param>
    public TuiState(T initialValue)
    {
        _value = initialValue;
    }

    /// <summary>
    /// Gets or sets the current value of the state.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TuiState<T>));

            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                var oldValue = _value;
                _value = value;
                OnPropertyChanged();
                OnValueChanged(oldValue, value);
            }
        }
    }

    /// <summary>
    /// Event raised when the PropertyChanged event occurs.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Event raised when the value changes, providing both old and new values.
    /// </summary>
    public event Action<T, T>? ValueChanged;

    /// <summary>
    /// Updates the value using a function that takes the current value and returns the new value.
    /// </summary>
    /// <param name="updater">Function that takes the current value and returns the new value.</param>
    public void Update(Func<T, T> updater)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TuiState<T>));

        Value = updater(_value);
    }

    /// <summary>
    /// Resets the state to its default value.
    /// </summary>
    public void Reset()
    {
        Value = default(T)!;
    }

    /// <summary>
    /// Creates a derived state that automatically updates when this state changes.
    /// </summary>
    /// <typeparam name="TResult">The type of the derived state.</typeparam>
    /// <param name="selector">Function to transform the current value to the derived value.</param>
    /// <returns>A new TuiState that tracks the derived value.</returns>
    public TuiState<TResult> Derive<TResult>(Func<T, TResult> selector)
    {
        var derivedState = new TuiState<TResult>(selector(_value));
        
        ValueChanged += (_, newValue) =>
        {
            if (!derivedState._isDisposed)
            {
                derivedState.Value = selector(newValue);
            }
        };

        return derivedState;
    }

    /// <summary>
    /// Subscribes to value changes with a callback.
    /// </summary>
    /// <param name="callback">Callback to invoke when the value changes.</param>
    /// <returns>A disposable subscription that can be used to unsubscribe.</returns>
    public IDisposable Subscribe(Action<T> callback)
    {
        void Handler(T _, T newValue) => callback(newValue);
        ValueChanged += Handler;
        
        // Call immediately with current value
        callback(_value);
        
        return new Subscription(() => ValueChanged -= Handler);
    }

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Raises the ValueChanged event.
    /// </summary>
    private void OnValueChanged(T oldValue, T newValue)
    {
        ValueChanged?.Invoke(oldValue, newValue);
    }

    /// <summary>
    /// Disposes the state and cleans up event subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        PropertyChanged = null;
        ValueChanged = null;
        
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Implicit conversion from TuiState to its value.
    /// </summary>
    public static implicit operator T(TuiState<T> state) => state.Value;

    /// <summary>
    /// Returns a string representation of the current value.
    /// </summary>
    public override string ToString() => _value?.ToString() ?? "null";
}

/// <summary>
/// Represents a disposable subscription.
/// </summary>
internal sealed class Subscription : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _isDisposed = false;

    public Subscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        _isDisposed = true;
        _unsubscribe();
        
        GC.SuppressFinalize(this);
    }
}
