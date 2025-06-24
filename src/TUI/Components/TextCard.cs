namespace MaxBot.TUI;

public class TextCard : ITuiCard
{
    private readonly string _text;

    public TextCard(string text)
    {
        _text = text;
    }

    public IRenderable Render()
    {
        return new Markup(_text);
    }
}
