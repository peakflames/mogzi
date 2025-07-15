// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Services.Diff;

/// <summary>
/// Generates a unified diff from two sequences of strings.
/// </summary>
public static class UnifiedDiffGenerator
{
    private static ILogger? s_logger;

    /// <summary>
    /// Sets the logger for debugging diff generation.
    /// </summary>
    public static void SetLogger(ILogger? logger)
    {
        s_logger = logger;
    }

    /// <summary>
    /// Generates a unified diff.
    /// </summary>
    /// <param name="originalContent">The original content.</param>
    /// <param name="modifiedContent">The modified content.</param>
    /// <param name="originalPath">The path of the original file.</param>
    /// <param name="modifiedPath">The path of the modified file.</param>
    /// <returns>A <see cref="UnifiedDiff"/> object.</returns>
    public static UnifiedDiff GenerateDiff(string originalContent, string modifiedContent, string originalPath = "a/file", string modifiedPath = "b/file")
    {
        s_logger?.LogDebug("Generating diff for {OriginalPath} -> {ModifiedPath}", originalPath, modifiedPath);

        var originalLines = SplitIntoLines(originalContent);
        var modifiedLines = SplitIntoLines(modifiedContent);

        var lcsResult = LongestCommonSubsequence.FindLcs(originalLines, modifiedLines);
        var hunks = GenerateHunks(originalLines, modifiedLines, lcsResult);

        s_logger?.LogDebug("Generated {HunkCount} hunks with {TotalLines} total diff lines",
            hunks.Count, hunks.Sum(h => h.Lines.Count));

        return new UnifiedDiff
        {
            OriginalFile = originalPath,
            ModifiedFile = modifiedPath,
            Hunks = hunks
        };
    }

    private static string[] SplitIntoLines(string content)
    {
        return content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    private static List<DiffHunk> GenerateHunks(string[] original, string[] modified, LcsResult lcsResult)
    {
        var hunks = new List<DiffHunk>();
        var changes = IdentifyChanges(original, modified, lcsResult);

        if (changes.Count == 0)
        {
            s_logger?.LogDebug("No changes identified, returning empty hunks");
            return hunks;
        }

        s_logger?.LogDebug("Processing {ChangeCount} changes into hunks", changes.Count);

        const int context = 3;
        var changeIndex = 0;
        while (changeIndex < changes.Count)
        {
            var hunkStartIndex = changeIndex;
            var hunkEndIndex = changeIndex;

            // Group nearby changes into the same hunk
            while (hunkEndIndex < changes.Count - 1)
            {
                var last = changes[hunkEndIndex];
                var next = changes[hunkEndIndex + 1];
                var lastLine = last.OriginalLineNumber ?? last.ModifiedLineNumber ?? 0;
                var nextLine = next.OriginalLineNumber ?? next.ModifiedLineNumber ?? 0;
                if (nextLine - lastLine > context * 2)
                {
                    break;
                }

                hunkEndIndex++;
            }

            var firstChange = changes[hunkStartIndex];
            var lastChange = changes[hunkEndIndex];

            // Calculate hunk boundaries with context
            var originalHunkStart = Math.Max(0, (firstChange.OriginalLineNumber ?? 1) - 1 - context);
            var modifiedHunkStart = Math.Max(0, (firstChange.ModifiedLineNumber ?? 1) - 1 - context);

            var originalHunkEnd = Math.Min(original.Length, (lastChange.OriginalLineNumber ?? 0) + context);
            var modifiedHunkEnd = Math.Min(modified.Length, (lastChange.ModifiedLineNumber ?? 0) + context);

            // Generate lines for this hunk
            var lines = new List<DiffLine>();
            var originalIndex = originalHunkStart;
            var modifiedIndex = modifiedHunkStart;
            var hunkChanges = changes.GetRange(hunkStartIndex, hunkEndIndex - hunkStartIndex + 1);
            var changeQueue = new Queue<DiffLine>(hunkChanges);

            // Process all lines in the hunk range
            while (originalIndex < originalHunkEnd || modifiedIndex < modifiedHunkEnd)
            {
                // Check if current position matches a change
                var isChange = changeQueue.Count > 0 &&
                               ((changeQueue.Peek().OriginalLineNumber != null && changeQueue.Peek().OriginalLineNumber - 1 == originalIndex) ||
                                (changeQueue.Peek().ModifiedLineNumber != null && changeQueue.Peek().ModifiedLineNumber - 1 == modifiedIndex));

                if (isChange)
                {
                    var change = changeQueue.Dequeue();
                    lines.Add(change);

                    // Advance the appropriate index
                    if (change.Type == DiffLineType.Added)
                    {
                        modifiedIndex++;
                    }
                    else if (change.Type == DiffLineType.Removed)
                    {
                        originalIndex++;
                    }
                }
                else
                {
                    // Add context lines when both indices are valid and within bounds
                    if (originalIndex < originalHunkEnd && modifiedIndex < modifiedHunkEnd &&
                        originalIndex < original.Length && modifiedIndex < modified.Length)
                    {
                        var contextLine = new DiffLine
                        {
                            Type = DiffLineType.Context,
                            Content = original[originalIndex],
                            OriginalLineNumber = originalIndex + 1,
                            ModifiedLineNumber = modifiedIndex + 1
                        };
                        lines.Add(contextLine);

                        originalIndex++;
                        modifiedIndex++;
                    }
                    else if (originalIndex < originalHunkEnd && originalIndex < original.Length)
                    {
                        // Only original content remains
                        originalIndex++;
                    }
                    else if (modifiedIndex < modifiedHunkEnd && modifiedIndex < modified.Length)
                    {
                        // Only modified content remains
                        modifiedIndex++;
                    }
                    else
                    {
                        // Both indices are at or beyond their limits
                        break;
                    }
                }
            }

            // Process any remaining changes that weren't matched to positions
            while (changeQueue.Count > 0)
            {
                var change = changeQueue.Dequeue();
                lines.Add(change);
            }

            var hunk = new DiffHunk
            {
                OriginalStart = originalHunkStart + 1,
                OriginalLength = originalHunkEnd - originalHunkStart,
                ModifiedStart = modifiedHunkStart + 1,
                ModifiedLength = modifiedHunkEnd - modifiedHunkStart,
                Lines = lines
            };

            if (lines.Count == 0)
            {
                s_logger?.LogWarning("Created empty hunk at OriginalStart: {OriginalStart}, ModifiedStart: {ModifiedStart}",
                    hunk.OriginalStart, hunk.ModifiedStart);
            }

            hunks.Add(hunk);

            changeIndex = hunkEndIndex + 1;
        }

        return hunks;
    }

    private static List<DiffLine> IdentifyChanges(string[] original, string[] modified, LcsResult lcsResult)
    {
        var changes = new List<DiffLine>();
        var commonLines = new Queue<CommonLine>(lcsResult.CommonLines);
        var originalIndex = 0;
        var modifiedIndex = 0;

        while (originalIndex < original.Length || modifiedIndex < modified.Length)
        {
            if (commonLines.Count > 0 && commonLines.Peek().OriginalIndex == originalIndex && commonLines.Peek().ModifiedIndex == modifiedIndex)
            {
                var commonLine = commonLines.Dequeue();
                originalIndex++;
                modifiedIndex++;
            }
            else
            {
                if (originalIndex < original.Length && (commonLines.Count == 0 || commonLines.Peek().OriginalIndex > originalIndex))
                {
                    var removedLine = new DiffLine { Type = DiffLineType.Removed, Content = original[originalIndex], OriginalLineNumber = originalIndex + 1 };
                    changes.Add(removedLine);
                    originalIndex++;
                }

                if (modifiedIndex < modified.Length && (commonLines.Count == 0 || commonLines.Peek().ModifiedIndex > modifiedIndex))
                {
                    var addedLine = new DiffLine { Type = DiffLineType.Added, Content = modified[modifiedIndex], ModifiedLineNumber = modifiedIndex + 1 };
                    changes.Add(addedLine);
                    modifiedIndex++;
                }
            }
        }

        s_logger?.LogDebug("Identified {ChangeCount} changes ({AddedCount} added, {RemovedCount} removed)",
            changes.Count,
            changes.Count(c => c.Type == DiffLineType.Added),
            changes.Count(c => c.Type == DiffLineType.Removed));

        return changes;
    }
}
