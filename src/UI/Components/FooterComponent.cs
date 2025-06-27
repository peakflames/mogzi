namespace UI.Components;

public class FooterComponent : TuiComponentBase
{
    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var panel = new Panel(new Text("Footer Component"))
            .Header("Footer")
            .Border(BoxBorder.Rounded);
        return Task.FromResult<IRenderable>(panel);
    }
}
