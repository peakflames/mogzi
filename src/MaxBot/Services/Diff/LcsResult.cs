// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace MaxBot.Services.Diff;

internal readonly record struct LcsResult(IReadOnlyList<CommonLine> CommonLines);

internal readonly record struct CommonLine
{
    public int OriginalIndex { get; init; }
    public int ModifiedIndex { get; init; }
    public string Content { get; init; }
}
