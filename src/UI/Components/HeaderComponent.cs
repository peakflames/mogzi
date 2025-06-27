namespace UI.Components;

public class HeaderComponent : TuiComponentBase
{
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<HeaderComponent>? _logger;

    public HeaderComponent(
        IAppService appService,
        HistoryManager historyManager,
        StateManager stateManager,
        ILogger<HeaderComponent>? logger = null)
    {
        _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        _historyManager = historyManager ?? throw new ArgumentNullException(nameof(historyManager));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _logger = logger;
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        try
        {
            var content = BuildHeaderContent(context);
            var title = BuildHeaderTitle(context);
            
            var panel = new Panel(content)
                .Header(title)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);
                
            return Task.FromResult<IRenderable>(panel);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering HeaderComponent");
            
            // Fallback rendering in case of errors
            var errorPanel = new Panel(new Markup("[red]Error loading header[/]"))
                .Header("MaxBot - Error")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);
                
            return Task.FromResult<IRenderable>(errorPanel);
        }
    }

    private string BuildHeaderTitle(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        
        // Base title
        var title = "MaxBot";
        
        // Add version info if there's space (wide terminals)
        if (terminalWidth >= 120)
        {
            title += " v1.0.0";
        }
        
        // Add connection status if there's space
        if (terminalWidth >= 100)
        {
            var connectionStatus = GetConnectionStatus();
            title += $" - {connectionStatus}";
        }
        
        return title;
    }

    private IRenderable BuildHeaderContent(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        var messageCount = _historyManager.GetCompletedMessages().Count;
        var operationStatus = GetOperationStatus();
        
        if (terminalWidth >= 150)
        {
            // Wide terminal - show all information
            return BuildWideHeaderContent(messageCount, operationStatus);
        }
        else if (terminalWidth >= 100)
        {
            // Medium terminal - show essential information
            return BuildMediumHeaderContent(messageCount, operationStatus);
        }
        else
        {
            // Narrow terminal - show only critical information
            return BuildNarrowHeaderContent(operationStatus);
        }
    }

    private IRenderable BuildWideHeaderContent(int messageCount, string operationStatus)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Status").Width(20))
            .AddColumn(new TableColumn("Messages").Width(15))
            .AddColumn(new TableColumn("Session").Width(20))
            .AddColumn(new TableColumn("Performance").Width(25));

        var sessionInfo = GetSessionInfo();
        var performanceInfo = GetPerformanceInfo();

        table.AddRow(
            $"[green]{operationStatus}[/]",
            $"[blue]{messageCount} messages[/]",
            $"[yellow]{sessionInfo}[/]",
            $"[cyan]{performanceInfo}[/]"
        );

        return table;
    }

    private IRenderable BuildMediumHeaderContent(int messageCount, string operationStatus)
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Status").Width(15))
            .AddColumn(new TableColumn("Messages").Width(15))
            .AddColumn(new TableColumn("Session").Width(20));

        var sessionInfo = GetSessionInfo();

        table.AddRow(
            $"[green]{operationStatus}[/]",
            $"[blue]{messageCount} msgs[/]",
            $"[yellow]{sessionInfo}[/]"
        );

        return table;
    }

    private IRenderable BuildNarrowHeaderContent(string operationStatus)
    {
        // For narrow terminals, just show status and message count
        var messageCount = _historyManager.GetCompletedMessages().Count;
        return new Markup($"[green]{operationStatus}[/] | [blue]{messageCount} msgs[/]");
    }

    private string GetConnectionStatus()
    {
        try
        {
            // Check if we can access the ChatClient (indicates service is available)
            var chatClient = _appService.ChatClient;
            return chatClient != null ? "Connected" : "Disconnected";
        }
        catch
        {
            return "Error";
        }
    }

    private string GetOperationStatus()
    {
        try
        {
            // Check if there are pending state changes (indicates processing)
            if (_stateManager.HasPendingChanges)
            {
                return "Processing";
            }
            
            // Check if we have any pending messages in history
            var pendingMessages = _historyManager.GetPendingMessages();
            if (pendingMessages.Any())
            {
                return "Active";
            }
            
            return "Ready";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error determining operation status");
            return "Unknown";
        }
    }

    private string GetSessionInfo()
    {
        try
        {
            var messageCount = _historyManager.GetCompletedMessages().Count;
            if (messageCount == 0)
            {
                return "New Session";
            }
            
            // Simple session duration approximation
            // In a real implementation, we might track session start time
            return $"Active ({messageCount} exchanges)";
        }
        catch
        {
            return "Session Info N/A";
        }
    }

    private string GetPerformanceInfo()
    {
        try
        {
            // This would ideally come from TuiApp statistics
            // For now, provide basic memory info
            var memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            return $"Memory: {memoryMB}MB";
        }
        catch
        {
            return "Perf: N/A";
        }
    }
}
