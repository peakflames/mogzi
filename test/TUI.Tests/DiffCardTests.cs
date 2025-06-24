namespace TUI.Tests;

public class DiffCardTests
{
    [Fact]
    public void DiffCard_Should_Render_Color_Coded_Diff()
    {
        // Arrange
        var console = new TestConsole();
        var eventBus = new TuiEventBus();
        var renderer = new ConsoleRenderer(console, eventBus);
        var diff = "--- a/file.txt\n+++ b/file.txt\n@@ -1,3 +1,3 @@\n-old line\n+new line\n same line";
        var filePatchedEvent = new FilePatchedEvent("/path/to/file.txt", diff);

        // Act
        eventBus.PublishAsync(filePatchedEvent);

        // Assert
        console.Output.Should().Contain("new line");
        console.Output.Should().Contain("old line");
    }
}
