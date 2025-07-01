namespace MaxBot.TUI.Components;

/// <summary>
/// Renders unified diffs with colored output, inspired by Gemini CLI's DiffRenderer component.
/// </summary>
public static class DiffRenderer
{
    /// <summary>
    /// Renders a unified diff with colored lines for additions, deletions, and context.
    /// </summary>
    /// <param name="diff">The unified diff to render</param>
    /// <returns>A renderable component showing the colored diff</returns>
    public static IRenderable RenderDiff(UnifiedDiff diff)
    {
        if (diff.Hunks.Count == 0)
        {
            return new Markup("[dim]No changes detected[/]");
        }

        var components = new List<IRenderable>
        {
            // Add file header
            CreateFileHeader(diff.OriginalFile, diff.ModifiedFile)
        };

        // Render each hunk
        foreach (var hunk in diff.Hunks)
        {
            components.Add(new Text(""));
            components.Add(RenderHunk(hunk));
        }

        return new Rows(components);
    }

    /// <summary>
    /// Creates a compact diff display suitable for inline use.
    /// </summary>
    /// <param name="diff">The unified diff to render</param>
    /// <param name="maxLines">Maximum number of lines to show (default: 10)</param>
    /// <returns>A compact renderable diff</returns>
    public static IRenderable RenderCompactDiff(UnifiedDiff diff, int maxLines = 10)
    {
        if (diff.Hunks.Count == 0)
        {
            return new Markup("[dim]No changes[/]");
        }

        var components = new List<IRenderable>();
        var lineCount = 0;

        // Add compact file header
        var fileName = Path.GetFileName(diff.ModifiedFile);
        components.Add(new Markup($"[bold]{fileName}[/]"));

        // Render lines from hunks up to maxLines
        foreach (var hunk in diff.Hunks)
        {
            if (lineCount >= maxLines)
            {
                break;
            }

            foreach (var line in hunk.Lines)
            {
                if (lineCount >= maxLines)
                {
                    components.Add(new Markup("[dim]... (truncated)[/]"));
                    break;
                }

                components.Add(RenderDiffLine(line, compact: true));
                lineCount++;
            }
        }

        return new Rows(components);
    }

    private static IRenderable CreateFileHeader(string originalFile, string modifiedFile)
    {
        var originalName = Path.GetFileName(originalFile);
        var modifiedName = Path.GetFileName(modifiedFile);

        if (originalName == modifiedName)
        {
            return new Markup($"[bold blue]📄 {modifiedName}[/]");
        }

        return new Markup($"[bold blue]📄 {originalName} → {modifiedName}[/]");
    }

    private static IRenderable RenderHunk(DiffHunk hunk)
    {
        var components = new List<IRenderable>();

        // Add hunk header with line numbers
        var hunkHeader = $"[dim]@@ -{hunk.OriginalStart},{hunk.OriginalLength} +{hunk.ModifiedStart},{hunk.ModifiedLength} @@[/]";
        components.Add(new Markup(hunkHeader));

        // Render each line in the hunk
        foreach (var line in hunk.Lines)
        {
            components.Add(RenderDiffLine(line));
        }

        return new Rows(components);
    }

    private static IRenderable RenderDiffLine(DiffLine line, bool compact = false)
    {
        var prefix = line.Type switch
        {
            DiffLineType.Added => "+",
            DiffLineType.Removed => "-",
            DiffLineType.Context => " ",
            _ => " "
        };

        var color = line.Type switch
        {
            DiffLineType.Added => "green",
            DiffLineType.Removed => "red",
            DiffLineType.Context => "dim",
            _ => "white"
        };

        var content = line.Content.EscapeMarkup();

        // In compact mode, truncate long lines
        if (compact && content.Length > 60)
        {
            content = content[..57] + "...";
        }

        // Show line numbers for non-compact mode
        if (!compact)
        {
            var lineNumber = line.Type switch
            {
                DiffLineType.Added => line.ModifiedLineNumber?.ToString() ?? "",
                DiffLineType.Removed => line.OriginalLineNumber?.ToString() ?? "",
                DiffLineType.Context => line.ModifiedLineNumber?.ToString() ?? "",
                _ => ""
            };

            var lineNumberPadded = lineNumber.PadLeft(4);
            return new Markup($"[dim]{lineNumberPadded}[/] [{color}]{prefix} {content}[/]");
        }

        return new Markup($"[{color}]{prefix} {content}[/]");
    }

    /// <summary>
    /// Renders a clean diff without borders, matching Gemini CLI's style with line numbers.
    /// </summary>
    /// <param name="diff">The unified diff to render</param>
    /// <returns>A clean renderable diff with line numbers</returns>
    public static IRenderable RenderCleanDiff(UnifiedDiff diff)
    {
        if (diff.Hunks.Count == 0)
        {
            return new Markup("[dim]No changes detected[/]");
        }

        // Check if all hunks are empty (have no lines)
        var totalLines = diff.Hunks.Sum(h => h.Lines?.Count ?? 0);
        if (totalLines == 0)
        {
            return new Markup("[dim]No changes detected (empty diff)[/]");
        }

        var components = new List<IRenderable>();

        // Render each hunk with line numbers (like Gemini)
        foreach (var hunk in diff.Hunks)
        {
            if (hunk.Lines?.Count > 0)
            {
                components.AddRange(RenderCleanHunk(hunk));
            }
        }

        // If no components were added, show no changes message
        if (components.Count == 0)
        {
            return new Markup("[dim]No changes detected (no valid hunks)[/]");
        }

        return new Rows(components);
    }

    /// <summary>
    /// Creates a diff summary showing the number of additions and deletions.
    /// </summary>
    /// <param name="diff">The unified diff to summarize</param>
    /// <returns>A renderable summary of the diff</returns>
    public static IRenderable CreateDiffSummary(UnifiedDiff diff)
    {
        var addedLines = 0;
        var removedLines = 0;

        foreach (var hunk in diff.Hunks)
        {
            foreach (var line in hunk.Lines)
            {
#pragma warning disable IDE0010 // Add missing cases
                switch (line.Type)
                {
                    case DiffLineType.Added:
                        addedLines++;
                        break;
                    case DiffLineType.Removed:
                        removedLines++;
                        break;
                    default:
                        break;
                }
#pragma warning restore IDE0010 // Add missing cases
            }
        }

        if (addedLines == 0 && removedLines == 0)
        {
            return new Markup("[dim]No changes[/]");
        }

        var parts = new List<string>();
        if (addedLines > 0)
        {
            parts.Add($"[green]+{addedLines}[/]");
        }
        if (removedLines > 0)
        {
            parts.Add($"[red]-{removedLines}[/]");
        }

        return new Markup($"[dim]({string.Join(" ", parts)})[/]");
    }

    /// <summary>
    /// Renders a clean hunk with line numbers on the left, matching Gemini CLI's style.
    /// </summary>
    private static IEnumerable<IRenderable> RenderCleanHunk(DiffHunk hunk)
    {
        var components = new List<IRenderable>();

        // Render each line in the hunk with line numbers
        foreach (var line in hunk.Lines)
        {
            components.Add(RenderCleanDiffLine(line));
        }

        return components;
    }

    /// <summary>
    /// Renders a single diff line with line number, prefix, and content.
    /// </summary>
    private static IRenderable RenderCleanDiffLine(DiffLine line)
    {
        var prefix = line.Type switch
        {
            DiffLineType.Added => "+",
            DiffLineType.Removed => "-",
            DiffLineType.Context => " ",
            _ => " "
        };

        var color = line.Type switch
        {
            DiffLineType.Added => "green",
            DiffLineType.Removed => "red",
            DiffLineType.Context => "white",
            _ => "white"
        };

        var content = line.Content.EscapeMarkup();

        // Get line number based on line type
        var lineNumber = line.Type switch
        {
            DiffLineType.Added => line.ModifiedLineNumber?.ToString() ?? "",
            DiffLineType.Removed => line.OriginalLineNumber?.ToString() ?? "",
            DiffLineType.Context => line.ModifiedLineNumber?.ToString() ?? line.OriginalLineNumber?.ToString() ?? "",
            _ => ""
        };

        // Format like Gemini: "  1  + const hello = () => {"
        var lineNumberPadded = lineNumber.PadLeft(3);
        return new Markup($"[dim]{lineNumberPadded}[/]  [{color}]{prefix} {content}[/]");
    }
}
