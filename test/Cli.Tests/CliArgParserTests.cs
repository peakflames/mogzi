using Xunit;
using FluentAssertions;

namespace Cli.Tests;

public class CliArgParserTests
{
    [Fact]
    public async Task Parse_WithNoArguments_ShouldShowHelp()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public async Task Parse_WithHelpArgument_ShouldShowHelp()
    {
        // Arrange
        var args = new string[] { "--help" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public async Task Parse_WithVersionArgument_ShouldShowVersion()
    {
        // Arrange
        var args = new string[] { "--version" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ShowVersion.Should().BeTrue();
    }

    [Fact]
    public async Task Parse_WithProfileArgument_ShouldSetProfileName()
    {
        // Arrange
        var args = new string[] { "--profile", "test-profile" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ProfileName.Should().Be("test-profile");
    }

    [Fact]
    public async Task Parse_WithConfigArgument_ShouldSetConfigPath()
    {
        // Arrange
        var args = new string[] { "--config", "test-config.json" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ConfigPath.Should().Be("test-config.json");
    }


    [Fact]
    public async Task Parse_WithStatusArgument_ShouldSetShowStatus()
    {
        // Arrange
        var args = new string[] { "--status" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ShowStatus.Should().BeTrue();
    }

    [Fact]
    public async Task Parse_WithPipedInput_ShouldCombinePipedInputAndPrompt()
    {
        // Arrange
        var args = new string[] { "summarize" };
        var pipedInput = "This is piped input.";
        var stringReader = new StringReader(pipedInput);
        Console.SetIn(stringReader);

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.UserPrompt.Should().Be($"{pipedInput}{Environment.NewLine}summarize");
    }

    [Theory]
    [InlineData("--tool-approvals", "readonly")]
    [InlineData("-ta", "readonly")]
    [InlineData("--tool-approvals", "all")]
    [InlineData("-ta", "all")]
    public async Task Parse_WithToolApprovalsArgument_ShouldSetToolApprovals(string flag, string value)
    {
        // Arrange
        var args = new string[] { flag, value };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.Value.ToolApprovals.Should().Be(value);
    }

    [Fact]
    public async Task Parse_WithInvalidToolApprovalsArgument_ShouldFail()
    {
        // Arrange
        var args = new string[] { "--tool-approvals", "invalid" };

        // Act
        var result = await CliArgParser.ParseAsync(args);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("Invalid value for --tool-approvals. Must be 'readonly' or 'all'.");
    }
}
