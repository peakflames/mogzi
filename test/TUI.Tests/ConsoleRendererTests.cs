using Xunit;
using MaxBot.TUI;
using Spectre.Console.Testing;

namespace TUI.Tests;

public class ConsoleRendererTests
{
    [Fact]
    public void Run_Should_Not_Throw()
    {
        // Arrange
        var console = new TestConsole();
        var renderer = new ConsoleRenderer();

        // Act
        var exception = Record.Exception(() => renderer.Run());

        // Assert
        Assert.Null(exception);
    }
}
