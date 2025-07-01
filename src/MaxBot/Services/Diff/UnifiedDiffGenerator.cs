// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using MaxBot.Domain.Diff;
using Microsoft.Extensions.Logging;

namespace MaxBot.Services.Diff;

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
        s_logger?.LogDebug("=== UnifiedDiffGenerator.GenerateDiff START ===");
        s_logger?.LogDebug("Original content length: {OriginalLength}, Modified content length: {ModifiedLength}", 
            originalContent?.Length ?? 0, modifiedContent?.Length ?? 0);
        
        var originalLines = SplitIntoLines(originalContent);
        var modifiedLines = SplitIntoLines(modifiedContent);

        s_logger?.LogDebug("Original lines: {OriginalLineCount}, Modified lines: {ModifiedLineCount}", 
            originalLines.Length, modifiedLines.Length);

        var lcsResult = LongestCommonSubsequence.FindLcs(originalLines, modifiedLines);
        s_logger?.LogDebug("LCS result - Common lines: {CommonLineCount}", lcsResult.CommonLines.Count);

        var hunks = GenerateHunks(originalLines, modifiedLines, lcsResult);
        s_logger?.LogDebug("Generated hunks: {HunkCount}", hunks.Count);

        if (hunks.Count > 0)
        {
            s_logger?.LogDebug("First hunk - OriginalStart: {OriginalStart}, ModifiedStart: {ModifiedStart}, Lines: {LineCount}", 
                hunks[0].OriginalStart, hunks[0].ModifiedStart, hunks[0].Lines.Count);
        }

        var result = new UnifiedDiff
        {
            OriginalFile = originalPath,
            ModifiedFile = modifiedPath,
            Hunks = hunks
        };

        s_logger?.LogDebug("=== UnifiedDiffGenerator.GenerateDiff END ===");
        return result;
    }

    private static string[] SplitIntoLines(string content)
    {
        return content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
    }

    private static List<DiffHunk> GenerateHunks(string[] original, string[] modified, LcsResult lcsResult)
    {
        s_logger?.LogDebug("=== GenerateHunks START ===");
        var hunks = new List<DiffHunk>();
        var changes = IdentifyChanges(original, modified, lcsResult);
        
        s_logger?.LogDebug("Identified changes: {ChangeCount}", changes.Count);
        for (int i = 0; i < Math.Min(changes.Count, 5); i++)
        {
            var change = changes[i];
            s_logger?.LogDebug("Change {Index}: Type={Type}, Content='{Content}', OriginalLine={OriginalLine}, ModifiedLine={ModifiedLine}", 
                i, change.Type, change.Content?.Length > 50 ? change.Content[..50] + "..." : change.Content, 
                change.OriginalLineNumber, change.ModifiedLineNumber);
        }
        
        if (changes.Count == 0)
        {
            s_logger?.LogDebug("No changes identified, returning empty hunks");
            return hunks;
        }

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
                    
                    s_logger?.LogDebug("Added change line: Type={Type}, Content='{Content}', OriginalLine={OriginalLine}, ModifiedLine={ModifiedLine}", 
                        change.Type, change.Content?.Length > 50 ? change.Content[..50] + "..." : change.Content, 
                        change.OriginalLineNumber, change.ModifiedLineNumber);
                    
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
                        
                        s_logger?.LogDebug("Added context line: Content='{Content}', OriginalLine={OriginalLine}, ModifiedLine={ModifiedLine}", 
                            contextLine.Content?.Length > 50 ? contextLine.Content[..50] + "..." : contextLine.Content, 
                            contextLine.OriginalLineNumber, contextLine.ModifiedLineNumber);
                        
                        originalIndex++;
                        modifiedIndex++;
                    }
                    else if (originalIndex < originalHunkEnd && originalIndex < original.Length)
                    {
                        // Only original content remains
                        s_logger?.LogDebug("Skipping original line {OriginalIndex} (no corresponding modified line)", originalIndex + 1);
                        originalIndex++;
                    }
                    else if (modifiedIndex < modifiedHunkEnd && modifiedIndex < modified.Length)
                    {
                        // Only modified content remains
                        s_logger?.LogDebug("Skipping modified line {ModifiedIndex} (no corresponding original line)", modifiedIndex + 1);
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
                s_logger?.LogDebug("Added remaining change line: Type={Type}, Content='{Content}', OriginalLine={OriginalLine}, ModifiedLine={ModifiedLine}", 
                    change.Type, change.Content?.Length > 50 ? change.Content[..50] + "..." : change.Content, 
                    change.OriginalLineNumber, change.ModifiedLineNumber);
            }

            var hunk = new DiffHunk
            {
                OriginalStart = originalHunkStart + 1,
                OriginalLength = originalHunkEnd - originalHunkStart,
                ModifiedStart = modifiedHunkStart + 1,
                ModifiedLength = modifiedHunkEnd - modifiedHunkStart,
                Lines = lines
            };

            s_logger?.LogDebug("Created hunk - OriginalStart: {OriginalStart}, ModifiedStart: {ModifiedStart}, Lines: {LineCount}", 
                hunk.OriginalStart, hunk.ModifiedStart, hunk.Lines.Count);
            
            if (hunk.Lines.Count > 0)
            {
                s_logger?.LogDebug("First line in hunk: Type={Type}, Content='{Content}'", 
                    hunk.Lines[0].Type, hunk.Lines[0].Content?.Length > 50 ? hunk.Lines[0].Content[..50] + "..." : hunk.Lines[0].Content);
            }

            hunks.Add(hunk);

            changeIndex = hunkEndIndex + 1;
        }

        s_logger?.LogDebug("=== GenerateHunks END - Total hunks: {HunkCount} ===", hunks.Count);
        return hunks;
    }

    private static List<DiffLine> IdentifyChanges(string[] original, string[] modified, LcsResult lcsResult)
    {
        s_logger?.LogDebug("=== IdentifyChanges START ===");
        s_logger?.LogDebug("Original array length: {OriginalLength}, Modified array length: {ModifiedLength}, Common lines: {CommonLineCount}", 
            original.Length, modified.Length, lcsResult.CommonLines.Count);

        var changes = new List<DiffLine>();
        var commonLines = new Queue<CommonLine>(lcsResult.CommonLines);
        var originalIndex = 0;
        var modifiedIndex = 0;

        while (originalIndex < original.Length || modifiedIndex < modified.Length)
        {
            if (commonLines.Count > 0 && commonLines.Peek().OriginalIndex == originalIndex && commonLines.Peek().ModifiedIndex == modifiedIndex)
            {
                var commonLine = commonLines.Dequeue();
                s_logger?.LogDebug("Skipping common line at original[{OriginalIndex}] = modified[{ModifiedIndex}]: '{Content}'", 
                    originalIndex, modifiedIndex, original[originalIndex].Length > 50 ? original[originalIndex][..50] + "..." : original[originalIndex]);
                originalIndex++;
                modifiedIndex++;
            }
            else
            {
                if (originalIndex < original.Length && (commonLines.Count == 0 || commonLines.Peek().OriginalIndex > originalIndex))
                {
                    var removedLine = new DiffLine { Type = DiffLineType.Removed, Content = original[originalIndex], OriginalLineNumber = originalIndex + 1 };
                    changes.Add(removedLine);
                    s_logger?.LogDebug("Added REMOVED line {LineNumber}: '{Content}'", 
                        originalIndex + 1, original[originalIndex].Length > 50 ? original[originalIndex][..50] + "..." : original[originalIndex]);
                    originalIndex++;
                }

                if (modifiedIndex < modified.Length && (commonLines.Count == 0 || commonLines.Peek().ModifiedIndex > modifiedIndex))
                {
                    var addedLine = new DiffLine { Type = DiffLineType.Added, Content = modified[modifiedIndex], ModifiedLineNumber = modifiedIndex + 1 };
                    changes.Add(addedLine);
                    s_logger?.LogDebug("Added ADDED line {LineNumber}: '{Content}'", 
                        modifiedIndex + 1, modified[modifiedIndex].Length > 50 ? modified[modifiedIndex][..50] + "..." : modified[modifiedIndex]);
                    modifiedIndex++;
                }
            }
        }

        s_logger?.LogDebug("=== IdentifyChanges END - Total changes: {ChangeCount} ===", changes.Count);
        return changes;
    }
}
