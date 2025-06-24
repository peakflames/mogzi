using Xunit;
using MaxBot.TUI;
using Spectre.Console.Testing;
using Spectre.Console;

namespace TUI.Tests;

public class TextCardTests
{
    [Fact]
    public void GetRenderable_Should_Return_Markup()
    {
        // Arrange
        var card = new TextCard("Hello");

        // Act
        var renderable = card.GetRenderable();

        // Assert
        Assert.IsType<Markup>(renderable);
    }
}
