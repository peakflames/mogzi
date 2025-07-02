// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using Mogzi.Domain.Diff;

namespace Mogzi.Services.Diff;

internal class LineOffsetStrategy : IFuzzyMatchingStrategy
{
    public string Name => "Line Offset Adjustment";

    public PatchResult TryApply(string content, UnifiedDiff patch)
    {
        for (var offset = -10; offset <= 10; offset++)
        {
            if (offset == 0)
            {
                continue;
            }

            var adjustedPatch = AdjustHunkLineNumbers(patch, offset);
            var result = new PatchApplicator().ApplyPatch(content, adjustedPatch);
            if (result.Success)
            {
                return result;
            }
        }

        return new PatchResult { Success = false, Error = "Could not find a suitable line offset." };
    }

    private UnifiedDiff AdjustHunkLineNumbers(UnifiedDiff patch, int offset)
    {
        var newHunks = new List<DiffHunk>();
        foreach (var hunk in patch.Hunks)
        {
            newHunks.Add(hunk with { OriginalStart = hunk.OriginalStart + offset });
        }
        return patch with { Hunks = newHunks };
    }
}
