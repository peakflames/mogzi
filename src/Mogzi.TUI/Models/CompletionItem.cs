namespace Mogzi.TUI.Models;

public class CompletionItem(string text, string description = "", CompletionItemType type = CompletionItemType.Text)
{
    public string Text { get; } = text;
    public string Description { get; } = description;
    public CompletionItemType Type { get; } = type;
}

public enum CompletionItemType
{
    Text,
    Command,
    File,
    Directory
}
