// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using Mogzi.Domain.Diff;

namespace Mogzi.Services.Diff;

internal class PatchApplicator
{
    public PatchResult ApplyPatch(string content, UnifiedDiff patch)
    {
        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();
        var appliedHunks = new List<AppliedHunk>();

        foreach (var hunk in patch.Hunks.OrderByDescending(h => h.OriginalStart))
        {
            var hunkResult = ApplyHunk(lines, hunk);
            if (!hunkResult.Success)
            {
                return new PatchResult
                {
                    Success = false,
                    Error = $"Failed to apply hunk at line {hunk.OriginalStart}: {hunkResult.Error}",
                    ConflictingHunk = hunk
                };
            }
            appliedHunks.Insert(0, hunkResult);
        }

        return new PatchResult
        {
            Success = true,
            ModifiedContent = string.Join(Environment.NewLine, lines),
            AppliedHunks = appliedHunks
        };
    }

    private HunkApplicationResult ApplyHunk(List<string> lines, DiffHunk hunk)
    {
        var targetLocation = FindHunkLocation(lines, hunk);
        if (targetLocation == -1)
        {
            return new HunkApplicationResult
            {
                Success = false,
                Error = "Could not find matching context for hunk"
            };
        }

        var removedLines = 0;
        var addedLines = 0;
        var currentPos = targetLocation;

        foreach (var diffLine in hunk.Lines)
        {
            switch (diffLine.Type)
            {
                case DiffLineType.Context:
                    if (currentPos >= lines.Count || !LinesMatch(lines[currentPos], diffLine.Content))
                    {
                        return new HunkApplicationResult
                        {
                            Success = false,
                            Error = $"Context mismatch at line {currentPos + 1}"
                        };
                    }
                    currentPos++;
                    break;

                case DiffLineType.Removed:
                    if (currentPos >= lines.Count || !LinesMatch(lines[currentPos], diffLine.Content))
                    {
                        return new HunkApplicationResult
                        {
                            Success = false,
                            Error = $"Line to remove not found at position {currentPos + 1}"
                        };
                    }
                    lines.RemoveAt(currentPos);
                    removedLines++;
                    break;

                case DiffLineType.Added:
                    lines.Insert(currentPos, diffLine.Content);
                    currentPos++;
                    addedLines++;
                    break;
            }
        }

        return new HunkApplicationResult
        {
            Success = true,
            LinesAdded = addedLines,
            LinesRemoved = removedLines,
            AppliedAtLine = targetLocation + 1
        };
    }

    private int FindHunkLocation(List<string> lines, DiffHunk hunk)
    {
        // Try to find the hunk by matching the pattern of context and removed lines
        var originalLines = hunk.Lines.Where(l => l.Type == DiffLineType.Context || l.Type == DiffLineType.Removed).ToList();

        if (originalLines.Count == 0)
        {
            // If there are no context or removed lines, we can only apply at the exact line number.
            return Math.Max(0, hunk.OriginalStart - 1);
        }

        // Search for the pattern in the file
        for (var i = 0; i <= lines.Count - originalLines.Count; i++)
        {
            var match = true;
            for (var j = 0; j < originalLines.Count; j++)
            {
                if (i + j >= lines.Count || !LinesMatch(lines[i + j], originalLines[j].Content))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private bool LinesMatch(string line1, string line2)
    {
        return line1.Trim() == line2.Trim();
    }
}
