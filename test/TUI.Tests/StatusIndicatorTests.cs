namespace TUI.Tests;

public class StatusIndicatorTests
{
    [Fact]
    public void ApiStatusIndicator_Should_Display_During_API_Call()
    {
        // Test case for FR-TUI-13: API Status Indicator
        // Verify animated indicator, token count, and elapsed time during API calls
        
        // Arrange
        var testConsole = new TestConsole();
        var tokenCount = 150;
        var elapsedTime = TimeSpan.FromSeconds(5);
        
        // Act
        var statusIndicator = new ApiStatusIndicator(tokenCount, elapsedTime);
        var renderable = statusIndicator.Render();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("API Call in progress", consoleOutput);
        Assert.Contains("150", consoleOutput); // Token count
        Assert.Contains("00:05", consoleOutput); // Elapsed time
        Assert.Contains("⏳", consoleOutput); // Progress indicator
    }

    [Fact]
    public void ApiStatusIndicator_Should_Update_Token_Count_And_Time()
    {
        // Test case for FR-TUI-13: API Status Indicator updates
        // Verify that token count and elapsed time update correctly
        
        // Arrange
        var testConsole = new TestConsole();
        var statusIndicator = new ApiStatusIndicator(0, TimeSpan.Zero);
        
        // Act
        statusIndicator.UpdateTokenCount(75);
        statusIndicator.UpdateElapsedTime(TimeSpan.FromSeconds(3));
        
        var renderable = statusIndicator.Render();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("75", consoleOutput); // Updated token count
        Assert.Contains("00:03", consoleOutput); // Updated elapsed time
    }

    [Fact]
    public void ApiStatusIndicator_Should_Hide_When_API_Call_Completes()
    {
        // Test case for FR-TUI-13: API Status Indicator completion
        // Verify that indicator is hidden when API call completes
        
        // Arrange
        var testConsole = new TestConsole();
        var statusIndicator = new ApiStatusIndicator(200, TimeSpan.FromSeconds(10));
        
        // Act
        statusIndicator.Complete();
        var renderable = statusIndicator.Render();
        testConsole.Write(renderable);
        
        // Assert
        var consoleOutput = testConsole.Output;
        Assert.Contains("API Call completed", consoleOutput);
        Assert.Contains("✓", consoleOutput); // Completion indicator
        Assert.Contains("200", consoleOutput); // Final token count
        Assert.Contains("00:10", consoleOutput); // Total time
    }
}
