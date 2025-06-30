namespace MaxBot.TUI.Models;

public class CompletionItem
{
    public string Text { get; }
    public string Description { get; }
    public CompletionItemType Type { get; }

    public CompletionItem(string text, string description = "", CompletionItemType type = CompletionItemType.Text)
    {
        Text = text;
        Description = description;
        Type = type;
    }
}

public enum CompletionItemType
{
    Text,
    Command,
    File,
    Directory
}
