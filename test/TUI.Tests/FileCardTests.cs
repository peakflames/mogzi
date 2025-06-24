namespace TUI.Tests;

public class FileCardTests
{
    [Fact]
    public void FileCard_Should_Render_File_Path()
    {
        // Arrange
        var console = new TestConsole();
        var eventBus = new TuiEventBus();
        var renderer = new ConsoleRenderer(console, eventBus);
        var filePath = "/path/to/some/file.txt";
        var fileReadEvent = new FileReadEvent(filePath);

        // Act
        eventBus.PublishAsync(fileReadEvent);

        // Assert
        console.Output.Should().Contain(filePath);
    }
}
