using Xunit;
using System.Threading.Tasks;
using FluentAssertions;
using System.IO;
using System;
using MaxBot;
using System.Text.RegularExpressions;
using CLI;

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

    [Fact]
    public async Task Run_WithPipedInput_ShouldReturnExpectedResponse()
    {
        // Arrange
        var args = new string[] { "summarize" };
        var pipedInput = "This is piped input.";
        var input = new StringReader(pipedInput);
        Console.SetIn(input);
        var output = new StringWriter();
        Console.SetOut(output);
        var testChatClient = new TestChatClient("Piped input summarized.");
        var clientResult = ChatClient.Create(testChatClient, "maxbot.config.json");
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();
        response.Should().Contain("Piped input summarized.");
    }

    [Theory]
    [InlineData("gpt")]
    [InlineData("gemini")]
    [InlineData("sonnet")]
    public async Task Run_ReadFile_WithLiveLlm_ShouldSucceedWithSingleApproval(string profileName)
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var fileContent = "This is a test file for the read_file scenario.";
        await File.WriteAllTextAsync(tempFile, fileContent);

        var args = new string[] { $"Read the content of the file at {tempFile} and tell me what it says." };
        // var input = new StringReader("y\n");
        // Console.SetIn(input);

        var output = new StringWriter();
        Console.SetOut(output);

        var clientResult = ChatClient.Create("maxbot.config.json", profileName, null, "oneshot", App.ConsoleWriteLLMResponseDetails);
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();

        // Check that the file content is in the response.
        response.Should().Contain(fileContent);

        // Clean up the temporary file.
        File.Delete(tempFile);
    }

    [Theory]
    [InlineData("gpt")]
    [InlineData("gemini")]
    [InlineData("sonnet")]
    public async Task Run_WriteFile_WithLiveLlm_ShouldSucceed(string profileName)
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var fileContent = "This is a test file for the write_file scenario.";

        var args = new string[] { $"Write the following content to the file at {tempFile}: {fileContent}" };

        var output = new StringWriter();
        Console.SetOut(output);

        var clientResult = ChatClient.Create("maxbot.config.json", profileName, null, "oneshot", App.ConsoleWriteLLMResponseDetails);
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        
        // Check that the file was created and has the correct content.
        var createdFileContent = await File.ReadAllTextAsync(tempFile);
        createdFileContent.Should().Be(fileContent);

        // Clean up the temporary file.
        File.Delete(tempFile);
    }


    [Theory]
    [InlineData("gpt")]
    [InlineData("gemini")]
    [InlineData("sonnet")]
    public async Task Run_WriteFile_WithToolApprovalsReadOnly_ShouldFail(string profileName)
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var fileContent = "This is a test file for the write_file scenario.";

        var args = new string[] { $"Write the following content to the file at {tempFile}: {fileContent}", "--tool-approvals", "readonly", "-p", profileName };

        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var exitCode = await Program.Run(args);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString().ToLower();
        response.Should().MatchRegex("read-?only");
        response.Should().MatchRegex("tool approval setting|not allowed");

        // Clean up the temporary files.
        File.Delete(tempFile);
    }
}
