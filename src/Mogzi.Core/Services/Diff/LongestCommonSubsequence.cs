// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Services.Diff;

/// <summary>
/// Implements the Longest Common Subsequence (LCS) algorithm.
/// </summary>
internal static class LongestCommonSubsequence
{
    /// <summary>
    /// Finds the longest common subsequence between two sequences of strings.
    /// </summary>
    /// <param name="original">The original sequence of strings.</param>
    /// <param name="modified">The modified sequence of strings.</param>
    /// <returns>An <see cref="LcsResult"/> containing the common lines.</returns>
    public static LcsResult FindLcs(string[] original, string[] modified)
    {
        var m = original.Length;
        var n = modified.Length;
        var dp = new int[m + 1, n + 1];

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                dp[i, j] = original[i - 1] == modified[j - 1] ? dp[i - 1, j - 1] + 1 : Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        return BacktrackLcs(original, modified, dp, m, n);
    }

    private static LcsResult BacktrackLcs(string[] original, string[] modified, int[,] dp, int i, int j)
    {
        var commonLines = new List<CommonLine>();

        while (i > 0 && j > 0)
        {
            if (original[i - 1] == modified[j - 1])
            {
                commonLines.Insert(0, new CommonLine
                {
                    OriginalIndex = i - 1,
                    ModifiedIndex = j - 1,
                    Content = original[i - 1]
                });
                i--;
                j--;
            }
            else if (dp[i - 1, j] > dp[i, j - 1])
            {
                i--;
            }
            else
            {
                j--;
            }
        }

        return new LcsResult(commonLines);
    }
}
