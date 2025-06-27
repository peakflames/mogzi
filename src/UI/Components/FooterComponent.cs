namespace UI.Components;

public class FooterComponent : TuiComponentBase
{
    private readonly IAppService _appService;
    private readonly HistoryManager _historyManager;
    private readonly StateManager _stateManager;
    private readonly ILogger<FooterComponent>? _logger;

    public FooterComponent(
        IAppService appService,
        HistoryManager historyManager,
        StateManager stateManager,
        ILogger<FooterComponent>? logger = null)
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
            var content = BuildFooterContent(context);
            var title = BuildFooterTitle(context);
            
            var panel = new Panel(content)
                .Header(title)
                .Border(BoxBorder.Rounded)
                .BorderColor(GetFooterBorderColor());
                
            return Task.FromResult<IRenderable>(panel);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error rendering FooterComponent");
            
            // Fallback rendering in case of errors
            var errorPanel = new Panel(new Text("[red]Error loading footer[/]"))
                .Header("Footer - Error")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red);
                
            return Task.FromResult<IRenderable>(errorPanel);
        }
    }

    private string BuildFooterTitle(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        var applicationStatus = GetApplicationStatus();
        
        // Base title with status
        var title = $"Status: {applicationStatus}";
        
        // Add active tool count if there are any and there's space
        var activeToolCount = GetActiveToolCount();
        if (activeToolCount > 0 && terminalWidth >= 100)
        {
            title += $" | Tools: {activeToolCount} active";
        }
        
        // Add help hint if there's space
        if (terminalWidth >= 140)
        {
            title += " | Press F1 for help";
        }
        
        return title;
    }

    private IRenderable BuildFooterContent(RenderContext context)
    {
        var terminalWidth = context.TerminalSize.Width;
        
        if (terminalWidth >= 160)
        {
            // Wide terminal - show all information
            return BuildWideFooterContent();
        }
        else if (terminalWidth >= 120)
        {
            // Medium terminal - show essential information
            return BuildMediumFooterContent();
        }
        else
        {
            // Narrow terminal - show only critical information
            return BuildNarrowFooterContent();
        }
    }

    private IRenderable BuildWideFooterContent()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Session").Width(25))
            .AddColumn(new TableColumn("Shortcuts").Width(35))
            .AddColumn(new TableColumn("Performance").Width(25))
            .AddColumn(new TableColumn("Network").Width(20));

        var sessionStats = GetSessionStatistics();
        var keyboardShortcuts = GetKeyboardShortcuts();
        var performanceMetrics = GetPerformanceMetrics();
        var networkStatus = GetNetworkStatus();

        table.AddRow(
            $"[blue]{sessionStats}[/]",
            $"[yellow]{keyboardShortcuts}[/]",
            $"[cyan]{performanceMetrics}[/]",
            $"[green]{networkStatus}[/]"
        );

        return table;
    }

    private IRenderable BuildMediumFooterContent()
    {
        var table = new Table()
            .Border(TableBorder.None)
            .AddColumn(new TableColumn("Session").Width(25))
            .AddColumn(new TableColumn("Shortcuts").Width(30))
            .AddColumn(new TableColumn("Performance").Width(25));

        var sessionStats = GetSessionStatistics();
        var keyboardShortcuts = GetKeyboardShortcuts();
        var performanceMetrics = GetPerformanceMetrics();

        table.AddRow(
            $"[blue]{sessionStats}[/]",
            $"[yellow]{keyboardShortcuts}[/]",
            $"[cyan]{performanceMetrics}[/]"
        );

        return table;
    }

    private IRenderable BuildNarrowFooterContent()
    {
        // For narrow terminals, show only essential status and shortcuts
        var applicationStatus = GetApplicationStatus();
        var essentialShortcuts = GetEssentialShortcuts();
        
        return new Text($"[green]{applicationStatus}[/] | [yellow]{essentialShortcuts}[/]");
    }

    private Color GetFooterBorderColor()
    {
        var status = GetApplicationStatus();
        
        return status switch
        {
            "Processing" => Color.Yellow,
            "Active" => Color.Blue,
            "Error" => Color.Red,
            _ => Color.Green // Ready state
        };
    }

    private string GetApplicationStatus()
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
            
            // Check connection status
            var isConnected = IsServiceConnected();
            if (!isConnected)
            {
                return "Error";
            }
            
            return "Ready";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error determining application status");
            return "Unknown";
        }
    }

    private int GetActiveToolCount()
    {
        try
        {
            // In a real implementation, this would track active tool executions
            // For now, we'll use pending messages as a proxy
            var pendingMessages = _historyManager.GetPendingMessages();
            return pendingMessages.Count();
        }
        catch
        {
            return 0;
        }
    }

    private string GetSessionStatistics()
    {
        try
        {
            var completedMessages = _historyManager.GetCompletedMessages();
            var userMessages = completedMessages.Count(m => m.Role == ChatRole.User);
            var assistantMessages = completedMessages.Count(m => m.Role == ChatRole.Assistant);
            
            if (completedMessages.Count == 0)
            {
                return "New session";
            }
            
            return $"{userMessages} queries, {assistantMessages} responses";
        }
        catch
        {
            return "Session: N/A";
        }
    }

    private string GetKeyboardShortcuts()
    {
        // REQ-UI-FOOTER-002: Help Integration - Contextual keyboard shortcuts
        var status = GetApplicationStatus();
        
        return status switch
        {
            "Processing" => "Esc: Cancel | Ctrl+C: Exit",
            "Active" => "↑/↓: History | Enter: Send | F1: Help",
            "Ready" => "↑/↓: History | Enter: Send | F1: Help | Ctrl+C: Exit",
            _ => "F1: Help | Ctrl+C: Exit"
        };
    }

    private string GetEssentialShortcuts()
    {
        // For narrow terminals, show only the most critical shortcuts
        var status = GetApplicationStatus();
        
        return status switch
        {
            "Processing" => "Esc: Cancel",
            _ => "Enter: Send | F1: Help"
        };
    }

    private string GetPerformanceMetrics()
    {
        try
        {
            // REQ-UI-FOOTER-003: Performance Metrics
            var memoryMB = GC.GetTotalMemory(false) / (1024 * 1024);
            
            // In debug mode, show more detailed metrics
            #if DEBUG
            var gen0Collections = GC.CollectionCount(0);
            var gen1Collections = GC.CollectionCount(1);
            var gen2Collections = GC.CollectionCount(2);
            return $"Mem: {memoryMB}MB | GC: {gen0Collections}/{gen1Collections}/{gen2Collections}";
            #else
            return $"Memory: {memoryMB}MB";
            #endif
        }
        catch
        {
            return "Performance: N/A";
        }
    }

    private string GetNetworkStatus()
    {
        try
        {
            // REQ-UI-FOOTER-003: Network connectivity status
            var isConnected = IsServiceConnected();
            
            if (isConnected)
            {
                return "Connected";
            }
            else
            {
                return "Disconnected";
            }
        }
        catch
        {
            return "Network: Unknown";
        }
    }

    private bool IsServiceConnected()
    {
        try
        {
            // Check if we can access the ChatClient (indicates service is available)
            var chatClient = _appService.ChatClient;
            return chatClient != null;
        }
        catch
        {
            return false;
        }
    }
}
