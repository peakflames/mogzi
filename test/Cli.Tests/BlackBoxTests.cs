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
        Console.SetIn(new StringReader(pipedInput));
        var output = new StringWriter();
        Console.SetOut(output);
        var testChatClient = new TestChatClient("Piped input summarized.");
        var clientResult = ChatClient.Create(testChatClient, "maxbot.config.json", null, "oneshot");
        clientResult.IsFailed.Should().Be(false);

        // Act
        var exitCode = await Program.Run(args, clientResult.Value);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString();
        response.Should().Contain("Piped input summarized.");
    }

    [Theory]
    // [InlineData("gpt")]
    [InlineData("gemini")]
    // [InlineData("sonnet")]
    public async Task Run_ReadFile_WithLiveLlm_ShouldSucceedWithSingleApproval(string profileName)
    {
        // Arrange
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, Path.GetRandomFileName());
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
        Directory.Delete(tempDir, true);
    }

    [Theory]
    // [InlineData("gpt")]
    [InlineData("gemini")]
    // [InlineData("sonnet")]
    public async Task Run_WriteFile_WithLiveLlm_ShouldSucceed(string profileName)
    {
        // Arrange
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, Path.GetRandomFileName());
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
        Directory.Delete(tempDir, true);
    }


    [Theory]
    // [InlineData("gpt")]
    [InlineData("gemini")]
    // [InlineData("sonnet")]
    public async Task Run_WriteFile_WithToolApprovalsReadOnly_ShouldFail(string profileName)
    {
        // Arrange
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, Path.GetRandomFileName());
        var fileContent = "This is a test file for the write_file scenario.";

        var args = new string[] { $"Write the following content to the file at {tempFile}: {fileContent}", "--tool-approvals", "readonly", "-p", profileName };

        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var exitCode = await Program.Run(args);

        // Assert
        exitCode.Should().Be(0);
        var response = output.ToString().ToLower();
        // The response should contain keywords indicating the operation was blocked due to permissions.
        response.Should().MatchRegex("read-?only|cannot write|disabled|not allowed");

        // Clean up the temporary files.
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public async Task Run_WithInvalidApiKey_ShouldReturnGracefulError()
    {
        // Arrange
        var tempDir = Path.Combine(Directory.GetCurrentDirectory(), "temp");
        Directory.CreateDirectory(tempDir);
        var tempConfigPath = Path.Combine(tempDir, "invalid_config.json");
        
        // Create a config file with an invalid API key
        var invalidConfig = """
        {
            "maxbotConfig": {
                "apiProviders": [ { "name": "TestProvider", "type": "OpenAI-Compatible", "apiKey": "invalid-key", "baseUrl": "https://api.openai.com/v1" } ],
                "profiles": [ { "default": true, "name": "Default", "apiProvider": "TestProvider", "modelId": "gpt-3.5-turbo" } ]
            }
        }
        """;
        await File.WriteAllTextAsync(tempConfigPath, invalidConfig);

        var args = new string[] { "hello", "--config", tempConfigPath };
        var output = new StringWriter();
        Console.SetOut(output);

        // Act
        var exitCode = await Program.Run(args);

        // Assert
        // The program should not crash, and should return a non-zero exit code to indicate failure.
        exitCode.Should().Be(1); 
        var response = output.ToString().ToLower();
        response.Should().Contain("error");
        response.Should().MatchRegex("api request|authentication|api key");

        // Clean up
        Directory.Delete(tempDir, true);
    }
}
