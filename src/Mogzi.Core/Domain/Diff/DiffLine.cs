// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Domain.Diff;

/// <summary>
/// Represents a single line in a diff hunk.
/// </summary>
public record DiffLine
{
    /// <summary>
    /// Gets the type of the diff line (e.g., Context, Added, Removed).
    /// </summary>
    public required DiffLineType Type { get; init; }

    /// <summary>
    /// Gets the content of the line.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the line number in the original file (if applicable).
    /// </summary>
    public int? OriginalLineNumber { get; init; }

    /// <summary>
    /// Gets the line number in the modified file (if applicable).
    /// </summary>
    public int? ModifiedLineNumber { get; init; }
}
