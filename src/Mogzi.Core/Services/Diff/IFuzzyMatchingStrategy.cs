// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Services.Diff;

internal interface IFuzzyMatchingStrategy
{
    string Name { get; }
    PatchResult TryApply(string content, UnifiedDiff patch);
}
