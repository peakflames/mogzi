namespace MaxBot.Prompts;

/// <summary>
/// Placeholder for user custom system prompt (future enhancement)
/// </summary>
internal static class UserCustomSystemPrompt
{
#pragma warning disable IDE0060 // Remove unused parameter
    public static string GetUserCustomPrompt(MaxbotConfiguration config)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // Future enhancement: Allow users to define custom system prompts in configuration
        // For now, return empty string
        return string.Empty;
    }
}
