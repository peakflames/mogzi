namespace MaxBot.TUI.Components;

/// <summary>
/// Displays tool execution results with enhanced visual feedback, including colored diffs and structured output.
/// Inspired by Gemini CLI's ToolGroupMessage component.
/// </summary>
public static class ToolExecutionDisplay
{
    /// <summary>
    /// Creates a visual display for tool execution with status indicator and optional diff.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed</param>
    /// <param name="status">The execution status</param>
    /// <param name="description">Optional description of what the tool is doing</param>
    /// <param name="diff">Optional diff to display for file changes</param>
    /// <param name="result">Optional result content to display</param>
    /// <returns>A renderable component showing the tool execution</returns>
    public static IRenderable CreateToolDisplay(
        string toolName,
        ToolExecutionStatus status,
        string? description = null,
        UnifiedDiff? diff = null,
        string? result = null)
    {
        var components = new List<IRenderable>();

        // Create header with tool name and status
        var header = CreateToolHeader(toolName, status, description);
        components.Add(header);

        // Add diff visualization if provided
        if (diff != null)
        {
            components.Add(new Text(""));
            components.Add(DiffRenderer.RenderDiff(diff));
        }

        // Add result content if provided
        if (!string.IsNullOrWhiteSpace(result))
        {
            components.Add(new Text(""));
            components.Add(CreateResultDisplay(result));
        }

        // Wrap in a bordered panel for visual separation
        var content = new Rows(components);
        return new Panel(content)
            .Border(BoxBorder.Rounded)
            .BorderColor(GetBorderColor(status))
            .Header($" {GetStatusIcon(status)} Tool Execution ")
            .HeaderAlignment(Justify.Left)
            .Padding(1, 0);
    }

    /// <summary>
    /// Creates a simple tool progress indicator for ongoing execution.
    /// </summary>
    /// <param name="toolName">The name of the tool being executed</param>
    /// <param name="description">Optional description of the current operation</param>
    /// <returns>A renderable progress indicator</returns>
    public static IRenderable CreateProgressIndicator(string toolName, string? description = null)
    {
        var animationFrame = DateTime.Now.Millisecond / 250 % 4;
        var spinner = animationFrame switch
        {
            0 => "⠋",
            1 => "⠙",
            2 => "⠹",
            3 => "⠸",
            _ => "⠋"
        };

        var text = !string.IsNullOrWhiteSpace(description) 
            ? $"{spinner} {toolName}: {description}"
            : $"{spinner} Executing {toolName}...";

        return new Markup($"[yellow]{text}[/]");
    }

    private static IRenderable CreateToolHeader(string toolName, ToolExecutionStatus status, string? description)
    {
        var statusIcon = GetStatusIcon(status);
        var statusColor = GetStatusColor(status);

        var headerText = !string.IsNullOrWhiteSpace(description)
            ? $"[{statusColor}]{statusIcon}[/] [bold]{toolName}[/] [dim]→ {description}[/]"
            : $"[{statusColor}]{statusIcon}[/] [bold]{toolName}[/]";

        return new Markup(headerText);
    }

    private static IRenderable CreateResultDisplay(string result)
    {
        // Try to parse XML tool response for better formatting
        if (result.StartsWith("<tool_response"))
        {
            return FormatXmlToolResponse(result);
        }

        // Fallback to simple text display
        return new Panel(new Markup(result.EscapeMarkup()))
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey23)
            .Header(" Result ")
            .Padding(1, 0);
    }

    private static IRenderable FormatXmlToolResponse(string xmlResponse)
    {
        try
        {
            // Simple XML parsing to extract key information
            var lines = new List<string>();

            if (xmlResponse.Contains("<notes>"))
            {
                var notesStart = xmlResponse.IndexOf("<notes>") + 7;
                var notesEnd = xmlResponse.IndexOf("</notes>");
                if (notesEnd > notesStart)
                {
                    var notes = xmlResponse[notesStart..notesEnd].Trim();
                    lines.AddRange(notes.Split('\n').Select(line => line.Trim()));
                }
            }

            if (xmlResponse.Contains("status=\"SUCCESS\""))
            {
                lines.Add("[green]✓ Operation completed successfully[/]");
            }
            else if (xmlResponse.Contains("status=\"FAILED\""))
            {
                lines.Add("[red]✗ Operation failed[/]");

                if (xmlResponse.Contains("<error>"))
                {
                    var errorStart = xmlResponse.IndexOf("<error>") + 7;
                    var errorEnd = xmlResponse.IndexOf("</error>");
                    if (errorEnd > errorStart)
                    {
                        var error = xmlResponse[errorStart..errorEnd].Trim();
                        lines.Add($"[red]Error: {error.EscapeMarkup()}[/]");
                    }
                }
            }

            var content = lines.Count > 0
                ? new Rows(lines.Select(line => new Markup(line)))
                : (IRenderable)new Markup(xmlResponse.EscapeMarkup());
            return new Panel(content)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey23)
                .Header(" Result ")
                .Padding(1, 0);
        }
        catch
        {
            // Fallback to raw display if XML parsing fails
            return new Panel(new Markup(xmlResponse.EscapeMarkup()))
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Grey23)
                .Header(" Result ")
                .Padding(1, 0);
        }
    }

    private static string GetStatusIcon(ToolExecutionStatus status)
    {
        return status switch
        {
            ToolExecutionStatus.Executing => "⏳",
            ToolExecutionStatus.Success => "✓",
            ToolExecutionStatus.Failed => "✗",
            ToolExecutionStatus.Confirming => "?",
            _ => "•"
        };
    }

    private static string GetStatusColor(ToolExecutionStatus status)
    {
        return status switch
        {
            ToolExecutionStatus.Executing => "yellow",
            ToolExecutionStatus.Success => "green",
            ToolExecutionStatus.Failed => "red",
            ToolExecutionStatus.Confirming => "blue",
            _ => "white"
        };
    }

    private static Color GetBorderColor(ToolExecutionStatus status)
    {
        return status switch
        {
            ToolExecutionStatus.Executing => Color.Yellow,
            ToolExecutionStatus.Success => Color.Green,
            ToolExecutionStatus.Failed => Color.Red,
            ToolExecutionStatus.Confirming => Color.Blue,
            _ => Color.Grey23
        };
    }
}
