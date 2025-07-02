namespace Mogzi.Prompts;

/// <summary>
/// Factory for creating model-specific system prompt components
/// </summary>
public static class SystemPromptComponentFactory
{
    /// <summary>
    /// Determines the model family based on the model ID
    /// </summary>
    public static ModelFamily GetModelFamily(string modelId)
    {
        var lowerModelId = modelId.ToLowerInvariant();

        if (lowerModelId.Contains("claude"))
        {
            return ModelFamily.Claude;
        }

        if (lowerModelId.Contains("gemini"))
        {
            return ModelFamily.Gemini;
        }

        return ModelFamily.Other;
    }

    /// <summary>
    /// Gets the model-specific prelude system prompt
    /// </summary>
    public static string GetModelSpecificPreludeSystemPrompt(ModelFamily modelFamily)
    {
        return modelFamily switch
        {
            ModelFamily.Claude => ClaudeSystemPrompts.GetPreludePrompt(),
            ModelFamily.Gemini => GeminiSystemPrompts.GetPreludePrompt(),
            _ => ClaudeSystemPrompts.GetPreludePrompt() // Default to Claude prompt for compatibility
        };
    }

    /// <summary>
    /// Gets the model-specific epilog system prompt
    /// </summary>
    public static string GetModelSpecificEpilogSystemPrompt(ModelFamily modelFamily, string absoluteWorkingDirectory)
    {
        return modelFamily switch
        {
            ModelFamily.Claude => ClaudeSystemPrompts.GetEpilogPrompt(absoluteWorkingDirectory),
            ModelFamily.Gemini => GeminiSystemPrompts.GetEpilogPrompt(absoluteWorkingDirectory),
            _ => ClaudeSystemPrompts.GetEpilogPrompt(absoluteWorkingDirectory) // Default to Claude prompt for compatibility
        };
    }
}
