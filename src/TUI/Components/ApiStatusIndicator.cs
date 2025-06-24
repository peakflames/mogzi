namespace MaxBot.TUI;

/// <summary>
/// Status indicator for API calls showing progress, token count, and elapsed time
/// </summary>
public class ApiStatusIndicator : ITuiCard
{
    private int _tokenCount;
    private TimeSpan _elapsedTime;
    private bool _isCompleted;
    private readonly DateTime _startTime;

    public ApiStatusIndicator(int tokenCount, TimeSpan elapsedTime)
    {
        _tokenCount = tokenCount;
        _elapsedTime = elapsedTime;
        _isCompleted = false;
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the token count
    /// </summary>
    public void UpdateTokenCount(int tokenCount)
    {
        _tokenCount = tokenCount;
    }

    /// <summary>
    /// Updates the elapsed time
    /// </summary>
    public void UpdateElapsedTime(TimeSpan elapsedTime)
    {
        _elapsedTime = elapsedTime;
    }

    /// <summary>
    /// Marks the API call as completed
    /// </summary>
    public void Complete()
    {
        _isCompleted = true;
    }

    public IRenderable Render()
    {
        if (_isCompleted)
        {
            return CreateCompletedIndicator();
        }

        return CreateActiveIndicator();
    }

    private IRenderable CreateActiveIndicator()
    {
        var statusText = new Markup("[yellow]⏳[/] API Call in progress...");
        
        var tokenInfo = new Markup($"[dim]Tokens:[/] [cyan]{_tokenCount}[/]");
        var timeInfo = new Markup($"[dim]Elapsed:[/] [cyan]{_elapsedTime:mm\\:ss}[/]");

        var content = new Columns(
            statusText,
            tokenInfo,
            timeInfo
        );

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("yellow"),
            Padding = new Padding(1, 0)
        };

        return panel;
    }

    private IRenderable CreateCompletedIndicator()
    {
        var completedText = new Markup("[green]✓ API Call completed[/]");
        var finalTokenInfo = new Markup($"[dim]Final tokens:[/] [cyan]{_tokenCount}[/]");
        var finalTimeInfo = new Markup($"[dim]Total time:[/] [cyan]{_elapsedTime:mm\\:ss}[/]");

        var content = new Columns(
            completedText,
            finalTokenInfo,
            finalTimeInfo
        );

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("green"),
            Padding = new Padding(1, 0)
        };

        return panel;
    }
}
