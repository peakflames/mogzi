namespace UI.Components;

public class DynamicContentComponent : TuiComponentBase
{
    private readonly HistoryManager _historyManager;

    public DynamicContentComponent(HistoryManager historyManager)
    {
        _historyManager = historyManager;
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var pendingMessages = _historyManager.GetPendingMessages();
        
        // REQ-UI-DYNAMIC-002: Content Organization
        // Group and organize pending operations for display
        var content = CreateDynamicContent(pendingMessages, context);
        
        // REQ-UI-DYNAMIC-001: Real-time Updates
        // Display current pending operations with appropriate header
        var headerText = pendingMessages.Count > 0 
            ? $"Active Operations ({pendingMessages.Count})"
            : "No Active Operations";
            
        var panel = new Panel(content)
            .Header(headerText)
            .Border(BoxBorder.Rounded);
            
        return Task.FromResult<IRenderable>(panel);
    }

    private IRenderable CreateDynamicContent(List<ChatMessage> pendingMessages, RenderContext context)
    {
        if (pendingMessages.Count == 0)
        {
            // REQ-UI-DYNAMIC-002: Content Organization - Empty state
            return CreateEmptyStateContent();
        }

        // REQ-UI-DYNAMIC-002: Content Organization - Group operations
        var operationGroups = GroupOperationsByType(pendingMessages);
        var contentItems = new List<IRenderable>();

        foreach (var group in operationGroups)
        {
            contentItems.Add(CreateOperationGroupContent(group.Key, group.Value, context));
        }

        return new Rows(contentItems);
    }

    private IRenderable CreateEmptyStateContent()
    {
        var emptyContent = new Markup("[dim]Waiting for operations...[/]");
        return emptyContent;
    }

    private Dictionary<string, List<ChatMessage>> GroupOperationsByType(List<ChatMessage> pendingMessages)
    {
        var groups = new Dictionary<string, List<ChatMessage>>();

        foreach (var message in pendingMessages)
        {
            var operationType = DetermineOperationType(message.Text);
            
            if (!groups.ContainsKey(operationType))
            {
                groups[operationType] = new List<ChatMessage>();
            }
            
            groups[operationType].Add(message);
        }

        return groups;
    }

    private string DetermineOperationType(string messageText)
    {
        // REQ-UI-DYNAMIC-002: Content Organization - Categorize operations
        if (messageText.StartsWith("Tool:", StringComparison.OrdinalIgnoreCase))
        {
            return "🔧 Tool Execution";
        }
        else if (messageText.Contains("Analyzing", StringComparison.OrdinalIgnoreCase) ||
                 messageText.Contains("Processing", StringComparison.OrdinalIgnoreCase))
        {
            return "🔍 Analysis";
        }
        else if (messageText.Contains("Generating", StringComparison.OrdinalIgnoreCase) ||
                 messageText.Contains("Creating", StringComparison.OrdinalIgnoreCase))
        {
            return "✨ Generation";
        }
        else
        {
            return "⚡ General";
        }
    }

    private IRenderable CreateOperationGroupContent(string groupName, List<ChatMessage> messages, RenderContext context)
    {
        var groupItems = new List<IRenderable>();
        
        // Add group header
        groupItems.Add(new Markup($"[bold]{groupName}[/]"));
        
        // Add operation items with responsive design
        foreach (var message in messages)
        {
            var operationContent = CreateOperationItemContent(message, context);
            groupItems.Add(operationContent);
        }
        
        // Add spacing between groups
        groupItems.Add(new Text(""));
        
        return new Rows(groupItems);
    }

    private IRenderable CreateOperationItemContent(ChatMessage message, RenderContext context)
    {
        // REQ-UI-DYNAMIC-001: Real-time Updates - Display operation details
        var operationText = FormatOperationText(message.Text, context);
        
        // Add visual indicator for active operations with simple text formatting
        var formattedText = $"[green]●[/] {operationText}";
        
        return new Markup(formattedText);
    }

    private string FormatOperationText(string messageText, RenderContext context)
    {
        // REQ-UI-DYNAMIC-001: Real-time Updates - Responsive text formatting
        var maxWidth = Math.Max(40, context.TerminalSize.Width - 10); // Reserve space for borders and indicators
        
        if (messageText.Length <= maxWidth)
        {
            return Markup.Escape(messageText);
        }
        
        // Truncate long messages for narrow terminals
        var truncated = messageText.Substring(0, maxWidth - 3) + "...";
        return Markup.Escape(truncated);
    }
}
