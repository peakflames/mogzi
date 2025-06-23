using MaxBot.Domain;
using MaxBot.Tools;
using Xunit;
using FluentAssertions;
using System.Xml;

namespace MaxBot.Tests.Tools;

public class SystemToolTests
{
    private SystemTools _systemTools;
    private MaxbotConfiguration _config;

    public SystemToolTests()
    {
        _config = new MaxbotConfiguration();
        _systemTools = new SystemTools(_config);
    }

    [Fact]
    public async Task ExecuteCommand_ShouldReturnCommandOutput_WhenSuccessful()
    {
        // Arrange
        var command = "echo hello world";

        // Act
        var result = await _systemTools.ExecuteCommand(command, requiresApproval: false);

        // Assert
        Assert.Contains("hello world", result);
    }

    [Fact]
    public async Task ExecuteCommand_ShouldReturnStderr_WhenCommandFails()
    {
        // Arrange
        var command = "non_existent_command";

        // Act
        var result = await _systemTools.ExecuteCommand(command, requiresApproval: false);

        // Assert
        Assert.Contains("Error:", result);
    }

    [Fact]
    public async Task ExecuteCommand_ShouldBeBlocked_WhenApprovalIsRequiredInReadonlyMode()
    {
        // Arrange
        _config.ToolApprovals = "readonly";
        _systemTools = new SystemTools(_config);
        var command = "echo readonly test";

        // Act
        var result = await _systemTools.ExecuteCommand(command, requiresApproval: true);

        // Assert
        Assert.Contains("Execution of this command requires approval", result);
    }

    [Fact]
    public async Task ExecuteCommand_ShouldSucceed_WhenApprovalIsRequiredInAllMode()
    {
        // Arrange
        _config.ToolApprovals = "all";
        _systemTools = new SystemTools(_config);
        var command = "echo all mode test";

        // Act
        var result = await _systemTools.ExecuteCommand(command, requiresApproval: true);

        // Assert
        Assert.Contains("all mode test", result);
    }

    [Fact]
    public async Task ExecuteCommand_WithNonExistentCommand_ShouldReturnShellError()
    {
        // Arrange
        var nonExistentCommand = Guid.NewGuid().ToString("N");
        _systemTools = new SystemTools(_config);

        // Act
        var result = await _systemTools.ExecuteCommand(nonExistentCommand, requiresApproval: false);

        // Assert
        result.Should().Contain("Error:");
        // Check for common "command not found" messages across platforms
        result.Should().MatchRegex("not recognized|command not found");
    }

    [Fact]
    public void AttemptCompletion_ShouldReturnCorrectXml()
    {
        // Arrange
        var resultText = "Task completed successfully.";
        var tools = new SystemTools(new MaxbotConfiguration());

        // Act
        var xmlString = tools.AttemptCompletion(resultText);

        // Assert
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlString);

        var toolResponseNode = xmlDoc.SelectSingleNode("tool_response");
        Assert.NotNull(toolResponseNode);
        Assert.Equal("attempt_completion", toolResponseNode.Attributes?["tool_name"]?.Value);

        var resultNode = xmlDoc.SelectSingleNode("//result");
        Assert.NotNull(resultNode);
        Assert.Equal("SUCCESS", resultNode.Attributes?["status"]?.Value);
    }
}
