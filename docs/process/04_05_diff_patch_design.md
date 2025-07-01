# MaxBot Diff/Patch System Design

## Core Components

### Domain Model
```csharp
public class UnifiedDiff
{
    public string OriginalFile { get; set; } = string.Empty;
    public string ModifiedFile { get; set; } = string.Empty;
    public List<DiffHunk> Hunks { get; set; } = [];
}

public class DiffHunk
{
    public int OriginalStart { get; set; }
    public int OriginalCount { get; set; }
    public int ModifiedStart { get; set; }
    public int ModifiedCount { get; set; }
    public List<DiffLine> Lines { get; set; } = [];
}

public enum DiffLineType { Context, Added, Removed }
```

### Patch Application
```csharp
public class PatchApplicator
{
    public PatchResult ApplyPatch(string content, UnifiedDiff patch)
    {
        var lines = content.Split(newlines, StringSplitOptions.None).ToList();
        
        // Apply hunks in reverse order to maintain line numbers
        foreach (var hunk in patch.Hunks.OrderByDescending(h => h.OriginalStart))
        {
            var hunkResult = ApplyHunk(lines, hunk);
            if (!hunkResult.Success)
                return new PatchResult { Success = false, Error = hunkResult.Error };
        }
        
        return new PatchResult { Success = true, ModifiedContent = string.Join(Environment.NewLine, lines) };
    }
}
```

## Fuzzy Matching

### Strategy Pattern
```csharp
public interface IFuzzyMatchingStrategy
{
    FuzzyMatchResult FindBestMatch(List<string> fileLines, List<DiffLine> hunkLines, int preferredLocation);
}

public class LineOffsetStrategy : IFuzzyMatchingStrategy
{
    // Searches within offset range around preferred location
}

public class WhitespaceNormalizationStrategy : IFuzzyMatchingStrategy
{
    // Normalizes whitespace differences for matching
}
```

### Longest Common Subsequence
- **Dynamic Programming**: Classic LCS algorithm for sequence comparison
- **Generic Implementation**: Works with any type T with custom equality comparers
- **Performance Optimization**: Efficient memory usage for large sequences
- **Match Quality Scoring**: Provides confidence scores for fuzzy matches

## Tool Integration

### DiffPatchTools
```csharp
public class DiffPatchTools
{
    public List<AIFunction> GetTools() => [
        AIFunctionFactory.Create(GenerateCodePatch, options),
        AIFunctionFactory.Create(ApplyCodePatch, options),
        AIFunctionFactory.Create(PreviewPatchApplication, options)
    ];
}
```

**Key Features:**
- **Security Validation**: All file operations validated within working directory
- **Structured XML Responses**: Detailed results with error information
- **Backup Creation**: Automatic backups before applying patches
- **Conflict Detection**: Identifies and reports patch application conflicts

## Result Tracking

### PatchResult Model
```csharp
public class PatchResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ModifiedContent { get; set; }
    public List<AppliedHunk> AppliedHunks { get; set; } = [];
    public DiffHunk? ConflictingHunk { get; set; }
}

public class AppliedHunk
{
    public int AppliedAtLine { get; set; }
    public int LinesAdded { get; set; }
    public int LinesRemoved { get; set; }
    public bool UsedFuzzyMatching { get; set; }
    public double MatchConfidence { get; set; }
}
```

This diff/patch system enables reliable code modifications with intelligent conflict resolution and comprehensive validation for MaxBot's AI-assisted development capabilities.
