using Xunit;
using Spectre.Console.Testing;
using MaxBot.TUI;

namespace TUI.Tests;

public class CommandCardTests
{
    [Fact]
    public void CommandCard_Should_Render_Command_With_Status()
    {
        // Test case for FR-TUI-4: Command Card
        // Verify that CommandCard displays command, status, and output correctly
        
        // Arrange
        var testConsole = new TestConsole();
        var command = "dotnet build";
        var status = CommandStatus.Running;
        var output = "Building project...";
        
        // Act
        var commandCard = new CommandCard(command, status, output);
        var renderable = commandCard.GetRenderable();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("dotnet build", consoleOutput);
        Assert.Contains("Running", consoleOutput);
        Assert.Contains("Building project...", consoleOutput);
    }

    [Fact]
    public void CommandCard_Should_Update_Status_Correctly()
    {
        // Test case for FR-TUI-4: Command Card status changes
        // Verify that CommandCard updates status from Running -> Success/Error
        
        // Arrange
        var testConsole = new TestConsole();
        var command = "dotnet test";
        var commandCard = new CommandCard(command, CommandStatus.Running, "");
        
        // Act
        commandCard.UpdateStatus(CommandStatus.Success);
        commandCard.UpdateOutput("Tests passed!");
        
        var renderable = commandCard.GetRenderable();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("Success", consoleOutput);
        Assert.Contains("Tests passed!", consoleOutput);
    }

    [Fact]
    public void CommandCard_Should_Display_Running_Indicator()
    {
        // Test case for FR-TUI-14: Command Status Indicator
        // Verify animated indicator when command is running
        
        // Arrange
        var testConsole = new TestConsole();
        var commandCard = new CommandCard("npm install", CommandStatus.Running, "Installing packages...");
        
        // Act
        var renderable = commandCard.GetRenderable();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("Running", consoleOutput);
        Assert.Contains("⏳", consoleOutput);
        Assert.Contains("npm install", consoleOutput);
    }

    [Fact]
    public void CommandCard_Should_Handle_Interrupt_Gracefully()
    {
        // Test case for NFR-TUI-3: Robustness
        // Verify CommandCard handles Ctrl+C interrupt without crashing
        
        // Arrange
        var testConsole = new TestConsole();
        var commandCard = new CommandCard("long-running-command", CommandStatus.Running, "Processing...");
        
        // Act
        commandCard.UpdateStatus(CommandStatus.Interrupted);
        var renderable = commandCard.GetRenderable();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("Interrupted", consoleOutput);
        Assert.Contains("⚠", consoleOutput);
        
        // Verify no exception is thrown when rendering after interrupt
        var exception = Record.Exception(() => commandCard.GetRenderable());
        Assert.Null(exception);
    }
}
