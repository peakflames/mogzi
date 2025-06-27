namespace UI.Components;

public class StaticHistoryComponent : TuiComponentBase
{
    private readonly HistoryManager _historyManager;

    public StaticHistoryComponent(HistoryManager historyManager)
    {
        _historyManager = historyManager;
    }

    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var messages = _historyManager.GetCompletedMessages();
        
        if (messages.Count == 0)
        {
            var emptyPanel = new Panel(new Text("No conversation history yet."))
                .Header("Static History")
                .Border(BoxBorder.Rounded);
            return Task.FromResult<IRenderable>(emptyPanel);
        }

        var messageRenderables = new List<IRenderable>();
        
        foreach (var message in messages)
        {
            var roleText = message.Role == ChatRole.User ? "[blue]User[/]" : "[green]Assistant[/]";
            var messageText = $"{roleText}: {message.Text}";
            messageRenderables.Add(new Markup(messageText));
        }

        var content = new Rows(messageRenderables);
        var panel = new Panel(content)
            .Header($"Static History ({messages.Count} messages)")
            .Border(BoxBorder.Rounded);
            
        return Task.FromResult<IRenderable>(panel);
    }
}
