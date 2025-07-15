namespace Mogzi.TUI.Components;

/// <summary>
/// Provides rendering utilities and helpers for TUI components.
/// Contains common formatting and rendering logic extracted from the original state classes.
/// </summary>
public class RenderingUtilities(ILogger<RenderingUtilities> logger) : IRenderingUtilities
{
    private readonly ILogger<RenderingUtilities> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public string FormatDisplayPath(string fullPath)
    {
        try
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (fullPath.StartsWith(homeDir))
            {
                return "~" + fullPath[homeDir.Length..].Replace('\\', '/');
            }
            return fullPath.Replace('\\', '/');
        }
        catch
        {
            return fullPath;
        }
    }

    public string FormatModelInfo(IAppService appService)
    {
        try
        {
            var chatClient = appService.ChatClient;
            var provider = chatClient.ActiveApiProvider.Name;
            var model = chatClient.ActiveProfile.ModelId;

            // Format like "provider/model" or just "model" if provider is empty
            if (!string.IsNullOrEmpty(provider))
            {
                return $"{provider}:{model}";
            }
            return model;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error getting model display info");
            return "unknown-model";
        }
    }

    public string FormatTokenUsage(IAppService appService, IEnumerable<ChatMessage> chatHistory)
    {
        try
        {
            var tokenCount = appService.CalculateTokenMetrics([.. chatHistory]);

            // Estimate context window size based on model (this could be made configurable)
            var contextWindowSize = EstimateContextWindowSize(appService);
            var percentageUsed = Math.Min(100.0, tokenCount * 100.0 / contextWindowSize);
            var percentageLeft = 100.0 - percentageUsed;

            return $"{tokenCount:N0} tokens, {percentageLeft:F2}% context left";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error calculating token usage");
            return "token calculation unavailable";
        }
    }

    public string FormatTokenNumber(long tokens)
    {
        return tokens switch
        {
            < 1000 => tokens.ToString(),
            < 10000 => $"{tokens / 1000.0:F1}k",
            < 1000000 => $"{tokens / 1000}k",
            _ => $"{tokens / 1000000.0:F1}m"
        };
    }

    public string FormatSessionTokenUsage(Session? session)
    {
        if (session?.UsageMetrics == null)
        {
            return "Tokens: --";
        }

        var metrics = session.UsageMetrics;
        var input = FormatTokenNumber(metrics.InputTokens);
        var output = FormatTokenNumber(metrics.OutputTokens);

        return $"Tokens: ↑ {input} ↓ {output}";
    }

    public string FormatCacheUsage(Session? session)
    {
        if (session?.UsageMetrics == null)
        {
            return "Cache: --";
        }

        // Future implementation when cache metrics available
        // var read = FormatTokenNumber(session.UsageMetrics.CacheReadTokens);
        // var write = FormatTokenNumber(session.UsageMetrics.CacheWriteTokens);
        // return $"Cache: © +{read} → {write}";

        return "Cache: --";
    }

    public string FormatContextWindowUsage(IAppService appService, IEnumerable<ChatMessage> chatHistory)
    {
        try
        {
            var currentTokens = appService.CalculateTokenMetrics([.. chatHistory]);
            var maxTokens = EstimateContextWindowSize(appService);
            var percentage = Math.Min(100.0, (double)currentTokens / maxTokens * 100);

            var colorTag = percentage switch
            {
                <= 50 => "[green]",
                <= 80 => "[yellow]",
                _ => "[red]"
            };

            var currentFormatted = FormatTokenNumber(currentTokens);
            var maxFormatted = FormatTokenNumber(maxTokens);

            return $"{colorTag}Context: {currentFormatted}/{maxFormatted} ({percentage:F0}%)[/]";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error calculating context window usage");
            return "[dim]Context: --[/]";
        }
    }

    public IRenderable RenderMessage(ChatMessage message, IThemeInfo? themeInfo = null)
    {
        if (string.IsNullOrEmpty(message.Text))
        {
            return new Text(string.Empty);
        }

        var messageType = GetMessageType(message);

        // Use theme information if provided, otherwise fall back to defaults
        var (prefix, color) = messageType switch
        {
            MessageType.User => (
                themeInfo?.UserMessagePrefix ?? "[dim]>[/] ",
                themeInfo?.UserMessageColor ?? "dim"
            ),
            MessageType.Assistant => (
                themeInfo?.AssistantMessagePrefix ?? "✦ ",
                themeInfo?.AssistantMessageColor ?? "skyblue1"
            ),
            MessageType.Tool => (
                "◉ ",
                "orange1"
            ),
            _ => (
                "",
                themeInfo?.SystemMessageColor ?? "white"
            )
        };

        // Strip system environment context from user messages for display
        var displayText = message.Role == ChatRole.User
            ? Mogzi.Utils.MessageUtils.StripSystemEnvironment(message.Text)
            : message.Text;

        return new Markup($"[{color}]{prefix}{Markup.Escape(displayText)}[/]");
    }

    private int EstimateContextWindowSize(IAppService appService)
    {
        try
        {
            var modelId = appService.ChatClient.ActiveProfile.ModelId.ToLowerInvariant();

            // Common model context window sizes
            return modelId switch
            {
                var m when m.Contains("gpt-4.1") => 1047576, // GPT-4.1 models
                var m when m.Contains("gpt-4") => 128000,  // GPT-4 Turbo models
                var m when m.Contains("gpt-3.5-") => 16385, // GPT-3.5 Turbo models
                var m when m.Contains("o3") => 200000, // o3 models
                var m when m.Contains("o4") => 200000, // o4 models
                var m when m.Contains("gemini-2.5") => 1048576, // Gemini 2.5 Flash models
                var m when m.Contains("gemini-1.5") => 1048576, // Gemini 1.5 Pro models
                var m when m.Contains("claude") => 200000, // Claude models
                _ => 128000 // Default fallback
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error estimating context window size");
            return 128000; // Safe default
        }
    }

    private MessageType GetMessageType(ChatMessage message)
    {
        if (message.Role == ChatRole.User)
        {
            return MessageType.User;
        }
        else if (message.Role == ChatRole.Assistant)
        {
            return MessageType.Assistant;
        }
        else if (message.Role == ChatRole.Tool)
        {
            return MessageType.Tool;
        }
        else
        {
            return MessageType.System;
        }
    }
}
