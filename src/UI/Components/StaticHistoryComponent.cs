namespace UI.Components;

public class StaticHistoryComponent : TuiComponentBase
{
    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var panel = new Panel(new Text("Static History Component"))
            .Header("Static History")
            .Border(BoxBorder.Rounded);
        return Task.FromResult<IRenderable>(panel);
    }
}
