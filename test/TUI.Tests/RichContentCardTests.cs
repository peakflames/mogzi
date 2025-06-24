using Xunit;
using Spectre.Console.Testing;
using MaxBot.TUI;

namespace TUI.Tests;

public class RichContentCardTests
{
    [Fact]
    public void RichContentCard_Should_Render_Basic_Markup()
    {
        // Arrange
        var content = "[bold]Hello[/] [green]World[/]!";
        var card = new RichContentCard(content);
        var console = new TestConsole();

        // Act
        console.Write(card.GetRenderable());
        var output = console.Output;

        // Assert
        Assert.Contains("Rich Content", output);
        Assert.Contains("Hello", output);
        Assert.Contains("World", output);
    }

    [Fact]
    public void RichContentCard_Should_Convert_Markdown_Lists()
    {
        // Arrange
        var content = "- Item 1\n- Item 2\n- Item 3";
        var card = new RichContentCard(content);
        var console = new TestConsole();

        // Act
        console.Write(card.GetRenderable());
        var output = console.Output;

        // Assert
        Assert.Contains("• Item 1", output);
        Assert.Contains("• Item 2", output);
        Assert.Contains("• Item 3", output);
    }

    [Fact]
    public void RichContentCard_Should_Convert_Markdown_Headings()
    {
        // Arrange
        var content = "# Main Heading\n## Sub Heading\nRegular text";
        var card = new RichContentCard(content);
        var console = new TestConsole();

        // Act
        console.Write(card.GetRenderable());
        var output = console.Output;

        // Assert
        Assert.Contains("Rich Content", output);
        Assert.Contains("Main Heading", output);
        Assert.Contains("Sub Heading", output);
        Assert.Contains("Regular text", output);
    }
}
