using Xunit;
using CLI;
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
}
