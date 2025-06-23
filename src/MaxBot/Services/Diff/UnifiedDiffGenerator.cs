// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using MaxBot.Domain.Diff;

namespace MaxBot.Services.Diff;

/// <summary>
/// Generates a unified diff from two sequences of strings.
/// </summary>
internal static class UnifiedDiffGenerator
{
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
        var originalLines = SplitIntoLines(originalContent);
        var modifiedLines = SplitIntoLines(modifiedContent);

        var lcsResult = LongestCommonSubsequence.FindLcs(originalLines, modifiedLines);
        var hunks = GenerateHunks(originalLines, modifiedLines, lcsResult);

        return new UnifiedDiff
        {
            OriginalFile = originalPath,
            ModifiedFile = modifiedPath,
            Hunks = hunks
        };
    }

    private static string[] SplitIntoLines(string content) => content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

    private static List<DiffHunk> GenerateHunks(string[] original, string[] modified, LcsResult lcsResult)
    {
        var hunks = new List<DiffHunk>();
        var changes = IdentifyChanges(original, modified, lcsResult);
        if (changes.Count == 0) return hunks;

        const int context = 3;
        var changeIndex = 0;
        while (changeIndex < changes.Count)
        {
            var hunkStartIndex = changeIndex;
            var hunkEndIndex = changeIndex;

            while (hunkEndIndex < changes.Count - 1)
            {
                var last = changes[hunkEndIndex];
                var next = changes[hunkEndIndex + 1];
                var lastLine = last.OriginalLineNumber ?? last.ModifiedLineNumber ?? 0;
                var nextLine = next.OriginalLineNumber ?? next.ModifiedLineNumber ?? 0;
                if (nextLine - lastLine > context * 2) break;
                hunkEndIndex++;
            }

            var firstChange = changes[hunkStartIndex];
            var lastChange = changes[hunkEndIndex];

            var originalHunkStart = Math.Max(0, (firstChange.OriginalLineNumber ?? 1) - 1 - context);
            var modifiedHunkStart = Math.Max(0, (firstChange.ModifiedLineNumber ?? 1) - 1 - context);

            var originalHunkEnd = Math.Min(original.Length, (lastChange.OriginalLineNumber ?? 0) + context);
            var modifiedHunkEnd = Math.Min(modified.Length, (lastChange.ModifiedLineNumber ?? 0) + context);

            var lines = new List<DiffLine>();
            var originalIndex = originalHunkStart;
            var modifiedIndex = modifiedHunkStart;
            var hunkChanges = changes.GetRange(hunkStartIndex, hunkEndIndex - hunkStartIndex + 1);
            var changeQueue = new Queue<DiffLine>(hunkChanges);

            while (originalIndex < originalHunkEnd || modifiedIndex < modifiedHunkEnd)
            {
                var isChange = changeQueue.Count > 0 &&
                               (changeQueue.Peek().OriginalLineNumber - 1 == originalIndex ||
                                changeQueue.Peek().ModifiedLineNumber - 1 == modifiedIndex);

                if (isChange)
                {
                    var change = changeQueue.Dequeue();
                    lines.Add(change);
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
                    if (originalIndex < originalHunkEnd && modifiedIndex < modifiedHunkEnd)
                    {
                        lines.Add(new DiffLine { Type = DiffLineType.Context, Content = original[originalIndex], OriginalLineNumber = originalIndex + 1, ModifiedLineNumber = modifiedIndex + 1 });
                        originalIndex++;
                        modifiedIndex++;
                    }
                    else if (originalIndex < originalHunkEnd)
                    {
                        originalIndex++;
                    }
                    else if (modifiedIndex < modifiedHunkEnd)
                    {
                        modifiedIndex++;
                    }
                }
            }

            hunks.Add(new DiffHunk
            {
                OriginalStart = originalHunkStart + 1,
                OriginalLength = originalHunkEnd - originalHunkStart,
                ModifiedStart = modifiedHunkStart + 1,
                ModifiedLength = modifiedHunkEnd - modifiedHunkStart,
                Lines = lines
            });

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
                commonLines.Dequeue();
                originalIndex++;
                modifiedIndex++;
            }
            else
            {
                if (originalIndex < original.Length && (commonLines.Count == 0 || commonLines.Peek().OriginalIndex > originalIndex))
                {
                    changes.Add(new DiffLine { Type = DiffLineType.Removed, Content = original[originalIndex], OriginalLineNumber = originalIndex + 1 });
                    originalIndex++;
                }

                if (modifiedIndex < modified.Length && (commonLines.Count == 0 || commonLines.Peek().ModifiedIndex > modifiedIndex))
                {
                    changes.Add(new DiffLine { Type = DiffLineType.Added, Content = modified[modifiedIndex], ModifiedLineNumber = modifiedIndex + 1 });
                    modifiedIndex++;
                }
            }
        }

        return changes;
    }
}
