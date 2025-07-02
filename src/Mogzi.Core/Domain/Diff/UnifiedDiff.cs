// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Domain.Diff;

/// <summary>
/// Represents a unified diff.
/// </summary>
public record UnifiedDiff
{
    /// <summary>
    /// Gets the path of the original file.
    /// </summary>
    public required string OriginalFile { get; init; }

    /// <summary>
    /// Gets the path of the modified file.
    /// </summary>
    public required string ModifiedFile { get; init; }

    /// <summary>
    /// Gets the list of hunks that make up the diff.
    /// </summary>
    public required IReadOnlyList<DiffHunk> Hunks { get; init; }
}
