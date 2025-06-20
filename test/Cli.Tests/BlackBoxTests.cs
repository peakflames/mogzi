using Xunit;
using System.Threading.Tasks;
using FluentAssertions;
using System.IO;
using System;
using MaxBot;

namespace Cli.Tests;

public class BlackBoxTests
{
    [Fact]
    public async Task Run_WithHelpArgument_ShouldDisplayHelp()
    {
        // Arrange
        var args = new string[] { "--help" };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var exitCode = await Program.Run(args);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();
        response.Should().Contain("Usage: max [prompt] [options]");
    }

    [Fact]
    public async Task Run_WithOneShotPrompt_ShouldReturnExpectedResponse()
    {
        // Arrange
        var args = new string[] { "hello" };
        var output = new StringWriter();
        Console.SetOut(output);
        var testChatClient = new TestChatClient("Hello, world!");
        var clientResult = ChatClient.Create(testChatClient, "maxbot.config.json");
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();
        response.Should().Contain("Hello, world!");
    }

    [Fact]
    public async Task Run_WithChatMode_ShouldReturnExpectedResponse()
    {
        // Arrange
        var args = new string[] { "--chat" };
        var input = new StringReader("hello\nexit\n");
        Console.SetIn(input);
        var output = new StringWriter();
        Console.SetOut(output);
        var testChatClient = new TestChatClient("Hello, world!");
        var clientResult = ChatClient.Create(testChatClient, "maxbot.config.json");
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();
        response.Should().Contain("Hello, world!");
    }
}
