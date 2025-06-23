// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

using MaxBot.Domain.Diff;

namespace MaxBot.Services.Diff;

internal record PatchResult
{
    public bool Success { get; init; }
    public string? ModifiedContent { get; init; }
    public string? Error { get; init; }
    public DiffHunk? ConflictingHunk { get; init; }
    public IReadOnlyList<AppliedHunk>? AppliedHunks { get; init; }
    public bool AppliedWithFuzzyMatching { get; set; }
    public string? FuzzyMatchingStrategy { get; set; }
    public string? OriginalError { get; init; }
    public int TotalLinesAdded => AppliedHunks?.Sum(h => h.LinesAdded) ?? 0;
    public int TotalLinesRemoved => AppliedHunks?.Sum(h => h.LinesRemoved) ?? 0;
}

internal record AppliedHunk
{
    public int LinesAdded { get; init; }
    public int LinesRemoved { get; init; }
    public int AppliedAtLine { get; init; }
}

internal record HunkApplicationResult : AppliedHunk
{
    public bool Success { get; init; }
    public string? Error { get; init; }
}
