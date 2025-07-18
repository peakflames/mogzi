using Mogzi.Domain;
using Mogzi.Tools;
using Xunit;
using FluentAssertions;
using System.Xml;

namespace Mogzi.Tests.Tools;

public class SystemToolTests
{
    private SystemTools _systemTools;
    private ApplicationConfiguration _config;

    public SystemToolTests()
    {
        _config = new ApplicationConfiguration();
        _systemTools = new SystemTools(_config);
    }

    [Fact]
    public async Task ExecuteCommand_ShouldReturnCommandOutput_WhenSuccessful()
    {
        // TOR-4.2
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
        // TOR-4.2, TOR-8.2
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
        // TOR-7.1
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
        // TOR-4.2, TOR-7.1
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
        // TOR-4.2, TOR-8.2
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
        // TOR-5.2
        // Arrange
        var resultText = "Task completed successfully.";
        var tools = new SystemTools(new ApplicationConfiguration());

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
