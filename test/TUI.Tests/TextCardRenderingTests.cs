namespace TUI.Tests;

public class TextCardRenderingTests
{
    [Fact]
    public void TextCard_Should_Render_Correctly()
    {
        // Arrange
        var console = new TestConsole();
        var card = new TextCard("Hello, World!");

        // Act
        console.Write(card.Render());

        // Assert
        Assert.Equal("Hello, World!", console.Output);
    }
}
