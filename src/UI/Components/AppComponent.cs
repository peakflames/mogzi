namespace UI.Components;

public class AppComponent : TuiComponentBase
{
    private readonly HeaderComponent _header;
    private readonly StaticHistoryComponent _staticHistory;
    private readonly DynamicContentComponent _dynamicContent;
    private readonly InputComponent _input;
    private readonly FooterComponent _footer;

    public AppComponent(
        HeaderComponent header,
        StaticHistoryComponent staticHistory,
        DynamicContentComponent dynamicContent,
        InputComponent input,
        FooterComponent footer)
    {
        _header = header;
        _staticHistory = staticHistory;
        _dynamicContent = dynamicContent;
        _input = input;
        _footer = footer;
    }

    public override async Task<IRenderable> RenderAsync(RenderContext context)
    {
        var renderables = new List<IRenderable>
        {
            await _header.RenderAsync(context),
            await _staticHistory.RenderAsync(context),
            await _dynamicContent.RenderAsync(context),
            await _input.RenderAsync(context),
            await _footer.RenderAsync(context)
        };

        return new Rows(renderables);
    }
}
