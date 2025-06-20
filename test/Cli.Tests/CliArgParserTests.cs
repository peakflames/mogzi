using Xunit;
using CLI;
using FluentAssertions;

namespace Cli.Tests;

public class CliArgParserTests
{
    [Fact]
    public void Parse_WithNoArguments_ShouldShowHelp()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithHelpArgument_ShouldShowHelp()
    {
        // Arrange
        var args = new string[] { "--help" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ShowHelp.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithVersionArgument_ShouldShowVersion()
    {
        // Arrange
        var args = new string[] { "--version" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ShowVersion.Should().BeTrue();
    }

    [Fact]
    public void Parse_WithProfileArgument_ShouldSetProfileName()
    {
        // Arrange
        var args = new string[] { "--profile", "test-profile" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ProfileName.Should().Be("test-profile");
    }

    [Fact]
    public void Parse_WithConfigArgument_ShouldSetConfigPath()
    {
        // Arrange
        var args = new string[] { "--config", "test-config.json" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ConfigPath.Should().Be("test-config.json");
    }

    [Fact]
    public void Parse_WithModeArgument_ShouldSetMode()
    {
        // Arrange
        var args = new string[] { "--mode", "oneshot" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.Mode.Should().Be("oneshot");
    }

    [Fact]
    public void Parse_WithUserPromptArgument_ShouldSetUserPrompt()
    {
        // Arrange
        var args = new string[] { "--userPrompt", "hello" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.UserPrompt.Should().Be("hello");
    }

    [Fact]
    public void Parse_WithStatusArgument_ShouldSetShowStatus()
    {
        // Arrange
        var args = new string[] { "--status" };

        // Act
        var result = CliArgParser.Parse(args);

        // Assert
        result.Value.ShowStatus.Should().BeTrue();
    }
}
