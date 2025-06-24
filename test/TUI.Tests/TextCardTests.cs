namespace TUI.Tests;

public class TextCardTests
{
    [Fact]
    public void Render_Should_Return_Markup()
    {
        // Arrange
        var card = new TextCard("Hello");

        // Act
        var renderable = card.Render();

        // Assert
        Assert.IsType<Markup>(renderable);
    }
}
