namespace MaxBot.PawPrints;

public class ScrollbackTerminal : IScrollbackTerminal
{
    private readonly IAnsiConsole _console;
    private readonly object _lock = new();
    private int _dynamicContentLineCount = 0;
    private int _updatableContentLineCount = 0;
    private bool _isShutdown = false;

    public ScrollbackTerminal(IAnsiConsole console)
    {
        _console = console;
    }

    public void Initialize()
    {
        _console.Clear();
        _console.Cursor.SetPosition(0, 0);
        _console.Cursor.Hide();
    }

    public void WriteStatic(IRenderable content, bool isUpdatable = false)
    {
        if (_isShutdown) return;

        lock (_lock)
        {
            ClearDynamicContent();
            ClearUpdatableContent();

            var writer = new StringWriter();
            var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), ColorSystem = ColorSystemSupport.NoColors });
            measuringConsole.Write(content);
            var output = writer.ToString();
            var lineCount = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

            if (isUpdatable)
            {
                _updatableContentLineCount = lineCount;
            }

            _console.Write(content);
            _console.WriteLine();
        }
    }

    public async Task StartDynamicDisplayAsync(Func<IRenderable> dynamicContentProvider, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_isShutdown) break;

            var dynamicContent = dynamicContentProvider();
            UpdateDynamic(dynamicContent);

            try
            {
                await Task.Delay(50, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    public void Shutdown()
    {
        if (_isShutdown) return;
        _isShutdown = true;

        lock (_lock)
        {
            ClearDynamicContent();
        }
        _console.Cursor.Show();
    }

    private void UpdateDynamic(IRenderable content)
    {
        if (_isShutdown) return;

        lock (_lock)
        {
            ClearDynamicContent();

            var writer = new StringWriter();
            var measuringConsole = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), ColorSystem = ColorSystemSupport.NoColors });
            measuringConsole.Write(content);
            var output = writer.ToString();
            _dynamicContentLineCount = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

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
                if (!_isShutdown) throw;
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
                if (!_isShutdown) throw;
            }
        }
        _updatableContentLineCount = 0;
    }
}
