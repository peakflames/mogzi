using Mogzi.Domain;
using Mogzi.Prompts;

namespace Mogzi.Tests.Prompts;

public class SystemPromptTests
{
    [Fact]
    public void GetModelFamily_ShouldReturnClaude_WhenModelIdContainsClaude()
    {
        // Arrange
        var modelId = "claude-3-5-sonnet-20241022";

        // Act
        var result = SystemPromptComponentFactory.GetModelFamily(modelId);

        // Assert
        Assert.Equal(ModelFamily.Claude, result);
    }

    [Fact]
    public void GetModelFamily_ShouldReturnGemini_WhenModelIdContainsGemini()
    {
        // Arrange
        var modelId = "gemini-1.5-pro";

        // Act
        var result = SystemPromptComponentFactory.GetModelFamily(modelId);

        // Assert
        Assert.Equal(ModelFamily.Gemini, result);
    }


    [Fact]
    public void GetModelFamily_ShouldReturnOther_WhenModelIdIsUnknown()
    {
        // Arrange
        var modelId = "unknown-model";

        // Act
        var result = SystemPromptComponentFactory.GetModelFamily(modelId);

        // Assert
        Assert.Equal(ModelFamily.Other, result);
    }

    [Fact]
    public void GetSystemPrompt_ShouldIncludeAbsolutePath_WhenRelativePathProvided()
    {
        // Arrange
        var config = new MaxbotConfiguration
        {
            ToolApprovals = "all",
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Default = true,
                    Name = "test",
                    ModelId = "claude-3-5-sonnet-20241022",
                    ApiProvider = "test"
                }
            }
        };

        var relativePath = "./test";
        var expectedAbsolutePath = Path.GetFullPath(relativePath);

        // Act
        var systemPrompt = Promptinator.GetSystemPrompt(
            "2025-01-07 19:44:00",
            "Linux",
            "bash",
            "testuser",
            "testhost",
            relativePath,
            config,
            "oneshot");

        // Assert
        Assert.Contains(expectedAbsolutePath, systemPrompt);
        Assert.DoesNotContain("./test", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_ShouldContainModelSpecificContent_ForClaudeModel()
    {
        // Arrange
        var config = new MaxbotConfiguration
        {
            ToolApprovals = "all",
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Default = true,
                    Name = "test",
                    ModelId = "claude-3-5-sonnet-20241022",
                    ApiProvider = "test"
                }
            }
        };

        // Act
        var systemPrompt = Promptinator.GetSystemPrompt(
            "2025-01-07 19:44:00",
            "Linux",
            "bash",
            "testuser",
            "testhost",
            "/home/test",
            config,
            "oneshot");

        // Assert
        Assert.Contains("Mogzi, created by Peakflames", systemPrompt);
        Assert.Contains("Max loves animals and hates emojis", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_ShouldContainModelSpecificContent_ForGeminiModel()
    {
        // Arrange
        var config = new MaxbotConfiguration
        {
            ToolApprovals = "all",
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Default = true,
                    Name = "test",
                    ModelId = "gemini-1.5-pro",
                    ApiProvider = "test"
                }
            }
        };

        // Act
        var systemPrompt = Promptinator.GetSystemPrompt(
            "2025-01-07 19:44:00",
            "Linux",
            "bash",
            "testuser",
            "testhost",
            "/home/test",
            config,
            "oneshot");

        // Assert
        Assert.Contains("You are Mogzi (Max), an advanced AI assistant created by Peakflames", systemPrompt);
        Assert.Contains("Stay focused on completing the user's request efficiently", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_ShouldContainToolUsageInstructions()
    {
        // Arrange
        var config = new MaxbotConfiguration
        {
            ToolApprovals = "all",
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Default = true,
                    Name = "test",
                    ModelId = "claude-3-5-sonnet-20241022",
                    ApiProvider = "test"
                }
            }
        };

        // Act
        var systemPrompt = Promptinator.GetSystemPrompt(
            "2025-01-07 19:44:00",
            "Linux",
            "bash",
            "testuser",
            "testhost",
            "/home/test",
            config,
            "oneshot");

        // Assert
        Assert.Contains("Tool Use Guidelines", systemPrompt);
        Assert.Contains("Mandatory Write Verification Protocol", systemPrompt);
        Assert.Contains("apply_code_patch", systemPrompt);
    }

    [Fact]
    public void GetSystemPrompt_ShouldContainEnvironmentInformation()
    {
        // Arrange
        var config = new MaxbotConfiguration
        {
            ToolApprovals = "readonly",
            Profiles = new List<Profile>
            {
                new Profile
                {
                    Default = true,
                    Name = "test",
                    ModelId = "claude-3-5-sonnet-20241022",
                    ApiProvider = "test"
                }
            }
        };

        // Act
        var systemPrompt = Promptinator.GetSystemPrompt(
            "2025-01-07 19:44:00",
            "Linux",
            "bash",
            "testuser",
            "testhost",
            "/home/test",
            config,
            "oneshot");

        // Assert
        Assert.Contains("User Environment", systemPrompt);
        Assert.Contains("operating system is Linux", systemPrompt);
        Assert.Contains("shell is bash", systemPrompt);
        Assert.Contains("username is testuser", systemPrompt);
        Assert.Contains("hostname is testhost", systemPrompt);
        Assert.Contains("current working directory absolute path is '/home/test'", systemPrompt);
        Assert.Contains("active mode is 'oneshot'", systemPrompt);
        Assert.Contains("Tool Approval Setting is 'readonly'", systemPrompt);
        Assert.Contains("current date is 2025-01-07 19:44:00", systemPrompt);
    }
}
