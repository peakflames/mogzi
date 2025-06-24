namespace MaxBot.TUI;

/// <summary>
/// A card that displays rich content using Spectre.Console markup.
/// Supports basic markdown-to-markup conversion for common formatting.
/// </summary>
public class RichContentCard : ITuiCard
{
    private readonly string _content;

    public RichContentCard(string content)
    {
        _content = content ?? string.Empty;
    }

    public IRenderable Render()
    {
        var processedContent = ConvertMarkdownToMarkup(_content);
        
        try
        {
            var markup = new Markup(processedContent);
            return new Panel(markup)
                .Header("Rich Content")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);
        }
        catch
        {
            // Fallback to plain text if markup parsing fails
            var text = new Text(processedContent);
            return new Panel(text)
                .Header("Rich Content")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue);
        }
    }

    /// <summary>
    /// Converts basic markdown syntax to Spectre.Console markup.
    /// This is a simple conversion for common formatting patterns.
    /// </summary>
    private static string ConvertMarkdownToMarkup(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        var result = markdown;

        // Convert **bold** to [bold]text[/]
        result = Regex.Replace(result, @"\*\*(.*?)\*\*", "[bold]$1[/]");
        
        // Convert *italic* to [italic]text[/]
        result = Regex.Replace(result, @"\*(.*?)\*", "[italic]$1[/]");
        
        // Convert # Heading to [bold underline]Heading[/]
        result = Regex.Replace(result, @"^# (.+)$", "[bold underline]$1[/]", RegexOptions.Multiline);
        
        // Convert ## Heading to [bold]Heading[/]
        result = Regex.Replace(result, @"^## (.+)$", "[bold]$1[/]", RegexOptions.Multiline);
        
        // Convert - List item to • List item
        result = Regex.Replace(result, @"^- (.+)$", "• $1", RegexOptions.Multiline);

        return result;
    }
}
