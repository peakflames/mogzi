namespace TUI.Tests;

public class ConsoleRendererTests
{
    [Fact]
    public void Constructor_Should_Not_Throw()
    {
        // Arrange
        var console = new TestConsole();
        var eventBus = new TuiEventBus();

        // Act
        var exception = Record.Exception(() => new ConsoleRenderer(console, eventBus));

        // Assert
        Assert.Null(exception);
    }
}
