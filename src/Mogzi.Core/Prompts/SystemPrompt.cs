
using System.Text;

namespace Mogzi.Prompts;

public static partial class Promptinator
{
    /// <summary>
    /// Generates a modular system prompt based on the model family and configuration
    /// </summary>
    public static string GetSystemPrompt(string currentDataTime, string userOperatingSystem, string userShell, string username, string hostname, string currentWorkingDirectory, ApplicationConfiguration config, string mode)
    {
        // Ensure we have an absolute path for the working directory
        var absoluteWorkingDirectory = Path.IsPathRooted(currentWorkingDirectory)
            ? currentWorkingDirectory
            : Path.GetFullPath(currentWorkingDirectory);

        // Get the active profile to determine model family
        var activeProfile = config.Profiles.FirstOrDefault(p => p.Default) ?? config.Profiles.FirstOrDefault();
        var modelFamily = activeProfile != null
            ? SystemPromptComponentFactory.GetModelFamily(activeProfile.ModelId)
            : ModelFamily.Claude; // Default fallback

        // Build the modular system prompt
        var promptBuilder = new StringBuilder();

        // 1. Model-specific prelude
        _ = promptBuilder.AppendLine(SystemPromptComponentFactory.GetModelSpecificPreludeSystemPrompt(modelFamily));
        _ = promptBuilder.AppendLine();

        // 2. Tool usage instructions (model-agnostic)
        _ = promptBuilder.AppendLine(ToolUsageSystemPrompt.GetToolUsagePrompt(absoluteWorkingDirectory));
        _ = promptBuilder.AppendLine();

        // 3. Environment information
        _ = promptBuilder.AppendLine(EnvSystemPrompt.GetEnvPrompt(
            currentDataTime,
            userOperatingSystem,
            userShell,
            username,
            hostname,
            absoluteWorkingDirectory,
            mode,
            config.ToolApprovals));
        _ = promptBuilder.AppendLine();

        // 4. User custom prompt (future enhancement)
        var userCustomPrompt = UserCustomSystemPrompt.GetUserCustomPrompt(config);
        if (!string.IsNullOrWhiteSpace(userCustomPrompt))
        {
            _ = promptBuilder.AppendLine(userCustomPrompt);
            _ = promptBuilder.AppendLine();
        }

        // 5. Model-specific epilog
        _ = promptBuilder.AppendLine(SystemPromptComponentFactory.GetModelSpecificEpilogSystemPrompt(modelFamily, absoluteWorkingDirectory));

        return promptBuilder.ToString().Trim();
    }
}
