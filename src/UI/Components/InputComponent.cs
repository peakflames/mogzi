namespace UI.Components;

public class InputComponent : TuiComponentBase
{
    public override Task<IRenderable> RenderAsync(RenderContext context)
    {
        var panel = new Panel(new Text("Input Component"))
            .Header("Input")
            .Border(BoxBorder.Rounded);
        return Task.FromResult<IRenderable>(panel);
    }
}
