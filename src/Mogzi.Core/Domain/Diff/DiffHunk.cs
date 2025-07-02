// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Domain.Diff;

/// <summary>
/// Represents a hunk of changes in a diff.
/// </summary>
public record DiffHunk
{
    /// <summary>
    /// Gets the starting line number in the original file.
    /// </summary>
    public int OriginalStart { get; set; }

    /// <summary>
    /// Gets the number of lines in the original file that this hunk covers.
    /// </summary>
    public int OriginalLength { get; set; }

    /// <summary>
    /// Gets the starting line number in the modified file.
    /// </summary>
    public int ModifiedStart { get; set; }

    /// <summary>
    /// Gets the number of lines in the modified file that this hunk covers.
    /// </summary>
    public int ModifiedLength { get; set; }

    /// <summary>
    /// Gets the collection of <see cref="DiffLine"/> objects that make up this hunk.
    /// </summary>
    public required IReadOnlyList<DiffLine> Lines { get; init; }
}
