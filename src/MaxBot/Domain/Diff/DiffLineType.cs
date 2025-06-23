// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace MaxBot.Domain.Diff;

/// <summary>
/// Defines the type of a line in a diff hunk.
/// </summary>
public enum DiffLineType
{
    /// <summary>
    /// An unchanged line, provided for context.
    /// </summary>
    Context,

    /// <summary>
    /// A line that has been added.
    /// </summary>
    Added,

    /// <summary>
    /// A line that has been removed.
    /// </summary>
    Removed,
}
