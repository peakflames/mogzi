using System.Text.RegularExpressions;

namespace Mogzi.Utils;

/// <summary>
/// Utility methods for handling message content and system environment context
/// </summary>
public static class MessageUtils
{
    /// <summary>
    /// Removes system environment XML blocks from a message for display purposes
    /// </summary>
    /// <param name="message">The message content that may contain system environment blocks</param>
    /// <returns>The message with system environment blocks removed</returns>
    public static string StripSystemEnvironment(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        // Remove <system_environment>...</system_environment> blocks
        var stripped = Regex.Replace(message, @"<system_environment>.*?</system_environment>", "",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Clean up any extra whitespace that might be left
        return stripped.Trim();
    }

    /// <summary>
    /// Appends system environment context to a user message
    /// </summary>
    /// <param name="userMessage">The original user message</param>
    /// <param name="envPrompt">The environment prompt to append</param>
    /// <returns>The combined message with environment context</returns>
    public static string AppendSystemEnvironment(string userMessage, string envPrompt)
    {
        if (string.IsNullOrEmpty(userMessage))
        {
            return userMessage;
        }

        if (string.IsNullOrEmpty(envPrompt))
        {
            return userMessage;
        }

        return $"{userMessage}\n\n{envPrompt}";
    }
}
