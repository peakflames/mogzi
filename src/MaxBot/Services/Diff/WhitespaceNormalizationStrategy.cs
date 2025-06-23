// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using System.Text.RegularExpressions;
using MaxBot.Domain.Diff;

namespace MaxBot.Services.Diff;

internal class WhitespaceNormalizationStrategy : IFuzzyMatchingStrategy
{
    public string Name => "Whitespace Normalization";

    public PatchResult TryApply(string content, UnifiedDiff patch)
    {
        var normalizedContent = NormalizeWhitespace(content);
        var normalizedPatch = NormalizePatchWhitespace(patch);

        var result = new PatchApplicator().ApplyPatch(normalizedContent, normalizedPatch);

        if (result.Success)
        {
            // This is a simplification. A real implementation would need to
            // restore whitespace more intelligently.
            result = result with { ModifiedContent = result.ModifiedContent };
        }

        return result;
    }

    private string NormalizeWhitespace(string content)
    {
        return Regex.Replace(content, @"\s+", " ").Trim();
    }

    private UnifiedDiff NormalizePatchWhitespace(UnifiedDiff patch)
    {
        var newHunks = new List<DiffHunk>();
        foreach (var hunk in patch.Hunks)
        {
            var newLines = new List<DiffLine>();
            foreach (var line in hunk.Lines)
            {
                newLines.Add(line with { Content = NormalizeWhitespace(line.Content) });
            }
            newHunks.Add(hunk with { Lines = newLines });
        }
        return patch with { Hunks = newHunks };
    }
}
