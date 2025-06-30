namespace MaxBot.Utils;

public static class ApiMetricUtils
{
    public static int GetSimplisticTokenCount(IEnumerable<ChatMessage> messages)
    {
        const int TokensPerMessage = 3;
        const int TokensPerRole = 1;
        const int BaseTokens = 3;
        var disallowedSpecial = new HashSet<string>();

        var tokenCount = BaseTokens;

        var encoding = SharpToken.GptEncoding.GetEncoding("cl100k_base");
        foreach (var message in messages)
        {
            tokenCount += TokensPerMessage;
            tokenCount += TokensPerRole;

            // Count tokens from message text (legacy support)
            if (!string.IsNullOrEmpty(message.Text))
            {
                tokenCount += encoding.Encode(message.Text, disallowedSpecial).Count;
            }

            // Count tokens from all content types in the message
            if (message.Contents != null)
            {
                foreach (var content in message.Contents)
                {
                    tokenCount += CountContentTokens(content, encoding, disallowedSpecial);
                }
            }
        }

        return tokenCount;
    }

    private static int CountContentTokens(AIContent content, SharpToken.GptEncoding encoding, HashSet<string> disallowedSpecial)
    {
        return content switch
        {
            // Function call content - count function name and arguments
            FunctionCallContent functionCall =>
                encoding.Encode(functionCall.CallId ?? string.Empty, disallowedSpecial).Count +
                encoding.Encode(functionCall.Name ?? string.Empty, disallowedSpecial).Count +
                CountFunctionArguments(functionCall.Arguments, encoding, disallowedSpecial) +
                10, // Additional overhead for function call structure

            // Function result content - count call ID and result
            FunctionResultContent functionResult =>
                encoding.Encode(functionResult.CallId ?? string.Empty, disallowedSpecial).Count +
                encoding.Encode(functionResult.Result?.ToString() ?? string.Empty, disallowedSpecial).Count +
                5, // Additional overhead for function result structure

            // Handle other known content types by type name (Native AOT compatible)
            _ when content.GetType().Name == "TextContent" => CountTextContentByToString(content, encoding, disallowedSpecial),
            _ when content.GetType().Name == "DataContent" => EstimateDataContentTokens(content),
            _ when content.GetType().Name == "ImageContent" => 150, // Standard image token estimate
            _ when content.GetType().Name == "UsageContent" => 5, // Minimal tokens for metadata

            // Default case for unknown content types
            _ => EstimateUnknownContentTokens(content, encoding, disallowedSpecial)
        };
    }

    private static int CountFunctionArguments(IDictionary<string, object?>? arguments, SharpToken.GptEncoding encoding, HashSet<string> disallowedSpecial)
    {
        if (arguments == null)
        {
            return 0;
        }

        // Estimate based on key-value pairs without dynamic serialization
        var estimatedTokens = 0;
        foreach (var kvp in arguments)
        {
            estimatedTokens += encoding.Encode(kvp.Key ?? string.Empty, disallowedSpecial).Count;
            estimatedTokens += encoding.Encode(kvp.Value?.ToString() ?? string.Empty, disallowedSpecial).Count;
            estimatedTokens += 2; // Add overhead for JSON structure (quotes, colon, comma)
        }
        return estimatedTokens;
    }

    private static int CountTextContentByToString(AIContent content, SharpToken.GptEncoding encoding, HashSet<string> disallowedSpecial)
    {
        // For TextContent, ToString() typically returns the text content
        var contentString = content.ToString() ?? string.Empty;

        // If ToString() returns type name, it's likely not the actual text content
        if (contentString.Contains("TextContent") || contentString.Length < 10)
        {
            // Fallback: estimate based on typical text content size
            return 20;
        }

        return encoding.Encode(contentString, disallowedSpecial).Count;
    }

    private static int EstimateDataContentTokens(AIContent content)
    {
        // For DataContent (images, files), use conservative estimates
        // Images in vision models typically consume 85-170 tokens per 512x512 tile
        // We'll use a moderate estimate since we can't inspect the actual content
        var contentString = content.ToString() ?? string.Empty;

        if (contentString.Contains("image") || contentString.Contains("png") ||
            contentString.Contains("jpg") || contentString.Contains("jpeg"))
        {
            return 150; // Standard image token estimate
        }
        else if (contentString.Contains("pdf") || contentString.Contains("text"))
        {
            return 50; // Text-based file estimate
        }

        return 25; // Default data content estimate
    }

    private static int EstimateUnknownContentTokens(AIContent content, SharpToken.GptEncoding encoding, HashSet<string> disallowedSpecial)
    {
        // For unknown content types, use ToString() and add a small overhead
        var contentString = content.ToString() ?? string.Empty;

        // If ToString() just returns the type name, use a conservative estimate
        if (contentString.Length < 20 && contentString.Contains("Content"))
        {
            return 10; // Conservative estimate for unknown content types
        }

        return encoding.Encode(contentString, disallowedSpecial).Count + 5; // Small overhead for structure
    }
}
