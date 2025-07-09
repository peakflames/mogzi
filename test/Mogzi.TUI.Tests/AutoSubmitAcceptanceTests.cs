namespace Mogzi.TUI.Tests;

/// <summary>
/// Systems-level acceptance tests for auto-submit functionality.
/// Tests the complete user workflow of piping input with auto-submit flag.
/// </summary>
public sealed class AutoSubmitAcceptanceTests : IDisposable
{
    public AutoSubmitAcceptanceTests()
    {
    }

    [Fact]
    public async Task AutoSubmit_WithPipedInput_ShouldAutomaticallySubmitMessage()
    {
        // Arrange
        var pipedInput = "Hello, tell me a joke. Note to myself: this is a test message for auto-submit";
        var args = new[] { "chat", "--auto-submit" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().BeOneOf(-1, 1);
        result.Output.Should().Contain("Hello, tell me a joke", 
            "piped input should appear in output"); // TOR-2.1
        
        // Verify the message was automatically submitted (should see AI thinking/response)
        result.Output.Should().MatchRegex(@"(Thinking|Processing|✦)", 
            "should show signs of AI processing the auto-submitted message"); // TOR-2.1
    }

    [Fact]
    public async Task AutoSubmit_WithoutFlag_ShouldNotAutoSubmit()
    {
        // Arrange
        var pipedInput = "Hello, tell me a joke. Note to myself: this is a test message for auto-submit without a flag";
        var args = new[] { "chat" }; // No --auto-submit flag

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().Be(-1, "application should be killed due to timeout (expected for interactive app)");
        result.Output.Should().Contain("Hello, tell me a joke", 
            "piped input should appear in input field");
        
        // Verify the message was NOT automatically submitted (should not see AI processing)
        result.Output.Should().NotMatchRegex(@"(Thinking|Processing|✦)", 
            "should not show signs of AI processing without auto-submit");
    }

    [Fact]
    public async Task AutoSubmit_ShortFlag_ShouldWork()
    {
        // Arrange
        var pipedInput = "Hello, tell me a joke. Note to myself: this is a test message for auto-submit with short flag";
        var args = new[] { "chat", "-a" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().Be(-1, "application should be killed due to timeout (expected for interactive app)");
        result.Output.Should().Contain("Hello, tell me a joke", 
            "piped input should appear in output");
        
        // Verify the message was automatically submitted
        result.Output.Should().MatchRegex(@"(Thinking|Processing|✦)", 
            "should show signs of AI processing with short flag");
    }

    [Fact]
    public async Task AutoSubmit_WithSession_ShouldLoadSessionAndAutoSubmit()
    {
        // Arrange
        var sessionName = $"AutoSubmitTest_{Guid.NewGuid():N}";
        var pipedInput = "Hello, tell me a joke. Note to myself: this is a test to load session with auto-submit";
        
        var args = new[] { "chat", "--session", sessionName, "--auto-submit" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().Be(-1, "application should be killed due to timeout (expected for interactive app)");
        result.Output.Should().Contain("Hello, tell me a joke", 
            "piped input should appear in output");
        
        // Verify session was created and message auto-submitted
        result.Output.Should().MatchRegex(@"(Creating new session|session)", 
            "should show session creation or loading");
        result.Output.Should().MatchRegex(@"(Thinking|Processing|✦)", 
            "should show signs of AI processing the auto-submitted message");
    }

    [Fact]
    public async Task AutoSubmit_EmptyPipedInput_ShouldNotCrash()
    {
        // Arrange
        var pipedInput = ""; // Empty input
        var args = new[] { "chat", "--auto-submit" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().Be(-1, "application should be killed due to timeout (expected for interactive app)");
        
        // Should not attempt to auto-submit empty input
        result.Output.Should().NotMatchRegex(@"(Thinking|Processing|✦)", 
            "should not process empty input");
    }

    [Fact]
    public async Task AutoSubmit_WhitespaceOnlyInput_ShouldNotCrash()
    {
        // Arrange
        var pipedInput = "   \n\t  \n  "; // Whitespace only
        var args = new[] { "chat", "--auto-submit" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationWithPipeAsync(args, pipedInput, TimeSpan.FromMilliseconds(10000));

        // Assert
        result.ExitCode.Should().Be(-1, "application should be killed due to timeout (expected for interactive app)");
        
        // Should not attempt to auto-submit whitespace-only input
        result.Output.Should().NotMatchRegex(@"(Thinking|Processing|✦)", 
            "should not process whitespace-only input");
    }

    [Fact]
    public async Task AutoSubmit_HelpFlag_ShouldShowAutoSubmitOption()
    {
        // Arrange
        var args = new[] { "chat", "--help" };

        // Act
        var result = await SystemsTestingHelpers.ExecuteApplicationAsync(args, timeout: TimeSpan.FromMilliseconds(5000));

        // Assert
        // Help command should exit normally, but may be killed due to timeout in some cases
        result.ExitCode.Should().BeOneOf(0, -1);
        result.Output.Should().Contain("--auto-submit", "help should mention auto-submit option");
        result.Output.Should().Contain("-a,", "help should mention short flag");
        result.Output.Should().Contain("Automatically submit piped input", 
            "help should explain auto-submit functionality");
    }

    public void Dispose()
    {
        // No cleanup needed for static helper methods
    }
}
