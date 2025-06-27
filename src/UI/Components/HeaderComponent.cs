namespace UI.Components;

public class HeaderComponent : TuiComponentBase
{
    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var panel = new Panel(new Text("Header Component"))
            .Header("Header")
            .Border(BoxBorder.Rounded);
        return Task.FromResult<IRenderable>(panel);
    }
}
