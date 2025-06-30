// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using MaxBot.Domain.Diff;

namespace MaxBot.Services.Diff;

internal class FuzzyPatchApplicator
{
    private readonly PatchApplicator _basicApplicator = new();
    private readonly List<IFuzzyMatchingStrategy> _strategies =
    [
        new WhitespaceNormalizationStrategy(),
        new LineOffsetStrategy(),
    ];

    public PatchResult TryApplyWithFuzzyMatching(string content, UnifiedDiff patch)
    {
        var exactResult = _basicApplicator.ApplyPatch(content, patch);
        if (exactResult.Success)
        {
            return exactResult;
        }

        foreach (var strategy in _strategies)
        {
            var fuzzyResult = strategy.TryApply(content, patch);
            if (fuzzyResult.Success)
            {
                fuzzyResult.AppliedWithFuzzyMatching = true;
                fuzzyResult.FuzzyMatchingStrategy = strategy.Name;
                return fuzzyResult;
            }
        }

        return new PatchResult
        {
            Success = false,
            Error = "Could not apply patch with any fuzzy matching strategy",
            OriginalError = exactResult.Error
        };
    }
}
