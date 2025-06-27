namespace UI.Components;

public class DynamicContentComponent : TuiComponentBase
{
    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var panel = new Panel(new Text("Dynamic Content Component"))
            .Header("Dynamic Content")
            .Border(BoxBorder.Rounded);
        return Task.FromResult<IRenderable>(panel);
    }
}
