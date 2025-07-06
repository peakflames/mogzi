namespace Mogzi.PawPrints;

public class ScrollbackTerminal(IAnsiConsole console) : IScrollbackTerminal, IDisposable
{
    private readonly IAnsiConsole _console = console;
    private readonly Lock _lock = new();
    private readonly ManualResetEventSlim _refreshSignal = new(false);
    private int _dynamicContentLineCount = 0;
    private int _updatableContentLineCount = 0;
    private bool _isDisposed = false;
    private bool _isShutdown = false;

    public void Initialize()
    {
        _console.Clear();
        _console.Cursor.SetPosition(0, 0);
        _console.Cursor.Hide();
    }

    public void WriteStatic(IRenderable content, bool isUpdatable = false)
    {
        if (_isShutdown)
        {
            return;
        }

        lock (_lock)
        {
            // Clear dynamic content before writing static content to prevent interference
            ClearDynamicContent();

            // Only clear updatable content if this is not an updatable write
            if (!isUpdatable)
            {
                ClearUpdatableContent();
            }

            var writer = new StringWriter();
            var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), ColorSystem = ColorSystemSupport.NoColors });
            measuringConsole.Write(content);
            var output = writer.ToString();
            var lineCount = output.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).Length;

            if (isUpdatable)
            {
                // For updatable content, clear the previous version first
                ClearUpdatableContent();
                _updatableContentLineCount = lineCount;
            }

            _console.Write(content);
            _console.WriteLine();
        }
    }

    public async Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && !_isDisposed)
        {
            var dynamicContent = dynamicContentProvider();
            UpdateDynamic(dynamicContent);

            try
            {
                // Wait for either the delay to complete or a refresh to be signaled
                _ = _refreshSignal.Wait(50, cancellationToken);
                _refreshSignal.Reset(); // Reset the signal after waiting
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        await Task.CompletedTask;
    }

    public void Shutdown()
    {
        if (_isShutdown)
        {
            return;
        }

        _isShutdown = true;

        lock (_lock)
        {
            ClearDynamicContent();
        }
        _console.Cursor.Show();
    }

    public void Refresh()
    {
        if (!_isDisposed)
        {
            // This is a non-blocking call to signal the event.
            _refreshSignal.Set();
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;
        _refreshSignal.Dispose();
        GC.SuppressFinalize(this);
    }

    private void UpdateDynamic(IRenderable content)
    {
        if (_isShutdown)
        {
            return;
        }

        lock (_lock)
        {
            ClearDynamicContent();

            var writer = new StringWriter();
            var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), ColorSystem = ColorSystemSupport.NoColors });
            measuringConsole.Write(content);
            var output = writer.ToString();
            _dynamicContentLineCount = output.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).Length;

            _console.Write(content);
        }
    }

    private void ClearDynamicContent()
    {
        if (_dynamicContentLineCount > 0)
        {
            try
            {
                _console.Cursor.MoveUp(_dynamicContentLineCount - 1);
                _console.Write("\x1b[0J");
            }
            catch (Exception)
            {
                if (!_isShutdown)
                {
                    throw;
                }
            }
        }
        _dynamicContentLineCount = 0;
    }

    private void ClearUpdatableContent()
    {
        if (_updatableContentLineCount > 0)
        {
            try
            {
                _console.Cursor.MoveUp(_updatableContentLineCount);
                _console.Write("\x1b[0J");
            }
            catch (Exception)
            {
                if (!_isShutdown)
                {
                    throw;
                }
            }
        }
        _updatableContentLineCount = 0;
    }
}
