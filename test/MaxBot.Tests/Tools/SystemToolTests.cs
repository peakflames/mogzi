using MaxBot.Domain;
using MaxBot.Tools;
using Xunit;

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
}
