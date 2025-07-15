// Copyright (c) 2024 Taylor Southwick. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.

namespace Mogzi.Tools;

/// <summary>
/// Provides tools for applying and generating diff patches.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DiffPatchTools"/> class.
/// </remarks>
/// <param name="config">The Mogzi configuration.</param>
/// <param name="llmResponseDetailsCallback">The callback for debug output.</param>
/// <param name="workingDirectoryProvider">The working directory provider.</param>
public class DiffPatchTools(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    private readonly FuzzyPatchApplicator _patchApplicator = new();

    public List<AIFunction> GetTools()
    {
        return
        [
            AIFunctionFactory.Create(
                ApplyCodePatch,
                new AIFunctionFactoryOptions
                {
                    Name = "apply_code_patch",
                    Description = "Apply code changes using Git-style unified diff patches. More robust than string replacement for handling whitespace and formatting variations."
                }),
            AIFunctionFactory.Create(
                GenerateCodePatch,
                new AIFunctionFactoryOptions
                {
                    Name = "generate_code_patch",
                    Description = "Generate a unified diff patch showing the changes between original and modified content."
                }),
            AIFunctionFactory.Create(
                PreviewPatchApplication,
                new AIFunctionFactoryOptions
                {
                    Name = "preview_patch_application",
                    Description = "Preview what changes a patch would make without actually applying them."
                })
        ];
    }

    /// <summary>
    /// Applies a code patch to a file.
    /// </summary>
    /// <param name="path">The path to the file to modify.</param>
    /// <param name="patch">The unified diff patch to apply.</param>
    /// <param name="useFuzzyMatching">Whether to use fuzzy matching if exact patch application fails.</param>
    /// <returns>A string containing the result of the operation.</returns>
    [Description("Apply code changes using Git-style unified diff patches. More robust than string replacement for handling whitespace and formatting variations.")]
    public string ApplyCodePatch(
        [Description("The path of the file to modify (relative to the current working directory)")]
        string path,
        [Description("The unified diff patch to apply. Can be generated from generate_code_patch or created manually.")]
        string patch,
        [Description("Whether to use fuzzy matching if exact patch application fails (default: true)")]
        bool useFuzzyMatching = true)
    {
        _llmResponseDetailsCallback?.Invoke($"Applying code patch to '{path}'{(useFuzzyMatching ? " with fuzzy matching" : "")}.", ConsoleColor.DarkGray);

        try
        {
            if (_config.ToolApprovals == "readonly")
            {
                return CreateErrorResponse("apply_code_patch", "Tool is in read-only mode. Cannot modify files.");
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(Path.Combine(workingDirectory, path));

            if (!absolutePath.StartsWith(workingDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return CreateErrorResponse("apply_code_patch", "Path is outside working directory");
            }

            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("apply_code_patch", $"File not found: {path}");
            }

            var originalContent = File.ReadAllText(absolutePath);
            var parsedPatch = ParseUnifiedDiff(patch);
            if (parsedPatch == null)
            {
                return CreateErrorResponse("apply_code_patch", "Invalid patch format. Expected unified diff format.");
            }

            var result = useFuzzyMatching
                ? _patchApplicator.TryApplyWithFuzzyMatching(originalContent, parsedPatch)
                : new PatchApplicator().ApplyPatch(originalContent, parsedPatch);

            if (!result.Success)
            {
                return CreatePatchFailureResponse(result, path);
            }

            File.WriteAllText(absolutePath, result.ModifiedContent);
            var newChecksum = ComputeSha256(result.ModifiedContent!);
            var verificationContent = File.ReadAllText(absolutePath);
            if (ComputeSha256(verificationContent) != newChecksum)
            {
                return CreateErrorResponse("apply_code_patch", "File verification failed after write");
            }

            return CreateSuccessResponse(path, absolutePath, result, ComputeSha256(originalContent), newChecksum, verificationContent);
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error applying code patch. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("apply_code_patch", $"Unexpected error: {ex.Message}");
        }
    }

    [Description("Generate a unified diff patch showing the changes between original and modified content.")]
    public string GenerateCodePatch(
        [Description("The path of the file to generate a patch for")]
        string path,
        [Description("The modified content that should replace the current file content")]
        string modifiedContent,
        [Description("Number of context lines to include around changes (default: 3)")]
        int contextLines = 3)
    {
        _llmResponseDetailsCallback?.Invoke($"Generating code patch for '{path}' with {contextLines} context lines.", ConsoleColor.DarkGray);

        try
        {
            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(Path.Combine(workingDirectory, path));

            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("generate_code_patch", $"File not found: {path}");
            }

            var originalContent = File.ReadAllText(absolutePath);
            var diff = UnifiedDiffGenerator.GenerateDiff(originalContent, modifiedContent, $"a/{path}", $"b/{path}");
            var patchText = FormatUnifiedDiff(diff);

            return $@"<tool_response tool_name=""generate_code_patch"">
    <notes>
        Generated unified diff patch for {path}
        Hunks: {diff.Hunks.Count}
        Total changes: {diff.Hunks.Sum(h => h.Lines.Count(l => l.Type != DiffLineType.Context))} lines
    </notes>
    <result status=""SUCCESS"" />
    <patch>{SecurityElement.Escape(patchText)}</patch>
</tool_response>";
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error generating code patch. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("generate_code_patch", $"Error generating patch: {ex.Message}");
        }
    }

    [Description("Preview what changes a patch would make without actually applying them.")]
    public string PreviewPatchApplication(
        [Description("The path of the file the patch would be applied to")]
        string path,
        [Description("The unified diff patch to preview")]
        string patch)
    {
        _llmResponseDetailsCallback?.Invoke($"Previewing patch application for '{path}'.", ConsoleColor.DarkGray);

        try
        {
            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(Path.Combine(workingDirectory, path));

            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("preview_patch_application", $"File not found: {path}");
            }

            var originalContent = File.ReadAllText(absolutePath);
            var parsedPatch = ParseUnifiedDiff(patch);
            if (parsedPatch == null)
            {
                return CreateErrorResponse("preview_patch_application", "Invalid patch format");
            }

            var result = _patchApplicator.TryApplyWithFuzzyMatching(originalContent, parsedPatch);

            var notes = new StringBuilder();
            _ = notes.AppendLine($"Patch preview for {path}");

            if (result.Success)
            {
                _ = notes.AppendLine("✓ Patch can be applied successfully");
                _ = notes.AppendLine($"Lines to be added: {result.TotalLinesAdded}");
                _ = notes.AppendLine($"Lines to be removed: {result.TotalLinesRemoved}");

                if (result.AppliedWithFuzzyMatching)
                {
                    _ = notes.AppendLine($"⚠ Requires fuzzy matching: {result.FuzzyMatchingStrategy}");
                }
            }
            else
            {
                _ = notes.AppendLine("✗ Patch cannot be applied");
                _ = notes.AppendLine($"Error: {result.Error}");
            }

            return $@"<tool_response tool_name=""preview_patch_application"">
    <notes>
        {SecurityElement.Escape(notes.ToString().Trim())}
    </notes>
    <result status=""{(result.Success ? "SUCCESS" : "FAILED")}"" />
    {(result.Success ? $"<preview_content>{SecurityElement.Escape(result.ModifiedContent)}</preview_content>" : "")}
</tool_response>";
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error previewing patch application. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("preview_patch_application", $"Error previewing patch: {ex.Message}");
        }
    }

    private string FormatUnifiedDiff(UnifiedDiff diff)
    {
        var builder = new StringBuilder();
        _ = builder.AppendLine($"--- {diff.OriginalFile}");
        _ = builder.AppendLine($"+++ {diff.ModifiedFile}");

        foreach (var hunk in diff.Hunks)
        {
            _ = builder.AppendLine($"@@ -{hunk.OriginalStart},{hunk.OriginalLength} +{hunk.ModifiedStart},{hunk.ModifiedLength} @@");
            foreach (var line in hunk.Lines)
            {
                var prefix = line.Type switch
                {
                    DiffLineType.Added => "+",
                    DiffLineType.Removed => "-",
                    _ => " "
                };
                _ = builder.AppendLine($"{prefix}{line.Content}");
            }
        }

        return builder.ToString();
    }

    private UnifiedDiff? ParseUnifiedDiff(string patch)
    {
        try
        {
            var lines = patch.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var hunks = new List<DiffHunk>();
            var currentHunkLines = new List<DiffLine>();
            string? originalFile = null;
            string? modifiedFile = null;
            var originalStart = 0;
            var modifiedStart = 0;
            var originalLength = 0;
            var modifiedLength = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("--- "))
                {
                    originalFile = line[4..].Split('\t')[0];
                    continue;
                }
                if (line.StartsWith("+++ "))
                {
                    modifiedFile = line[4..].Split('\t')[0];
                    continue;
                }
                if (line.StartsWith("@@ "))
                {
                    if (currentHunkLines.Any())
                    {
                        hunks.Add(new DiffHunk { Lines = currentHunkLines, OriginalStart = originalStart, OriginalLength = originalLength, ModifiedStart = modifiedStart, ModifiedLength = modifiedLength });
                        currentHunkLines = [];
                    }

                    var match = System.Text.RegularExpressions.Regex.Match(line, @"^@@ -(\d+)(,(\d+))? \+(\d+)(,(\d+))? @@");
                    if (!match.Success)
                    {
                        if (_config.Debug)
                        {
                            _llmResponseDetailsCallback?.Invoke($"ERROR: Failed to parse hunk header: {line}", ConsoleColor.Red);
                        }
                        return null;
                    }

                    originalStart = int.Parse(match.Groups[1].Value);
                    originalLength = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 1;
                    modifiedStart = int.Parse(match.Groups[4].Value);
                    modifiedLength = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 1;
                    continue;
                }

                if (line.StartsWith("+"))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Added, Content = line[1..] });
                }
                else if (line.StartsWith("-"))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Removed, Content = line[1..] });
                }
                else if (line.StartsWith(" "))
                {
                    currentHunkLines.Add(new DiffLine { Type = DiffLineType.Context, Content = line[1..] });
                }
            }

            if (currentHunkLines.Any())
            {
                hunks.Add(new DiffHunk { Lines = currentHunkLines, OriginalStart = originalStart, OriginalLength = originalLength, ModifiedStart = modifiedStart, ModifiedLength = modifiedLength });
            }

            if (originalFile == null || modifiedFile == null)
            {
                if (_config.Debug)
                {
                    _llmResponseDetailsCallback?.Invoke("ERROR: Patch did not contain file headers.", ConsoleColor.Red);
                }
                return null;
            }

            return new UnifiedDiff { OriginalFile = originalFile, ModifiedFile = modifiedFile, Hunks = hunks };
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error parsing unified diff patch. {ex.Message}", ConsoleColor.Red);
            }
            return null;
        }
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, PatchResult result, string originalChecksum, string newChecksum, string finalContent)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Successfully applied patch to {relativePath}");
        _ = notes.AppendLine($"Lines added: {result.TotalLinesAdded}");
        _ = notes.AppendLine($"Lines removed: {result.TotalLinesRemoved}");
        _ = notes.AppendLine($"Hunks applied: {result.AppliedHunks!.Count}");

        if (result.AppliedWithFuzzyMatching)
        {
            _ = notes.AppendLine($"Applied using fuzzy matching strategy: {result.FuzzyMatchingStrategy}");
        }

        return $@"<tool_response tool_name=""apply_code_patch"">
    <notes>
        {SecurityElement.Escape(notes.ToString().Trim())}
    </notes>
    <result status=""SUCCESS"" absolute_path=""{absolutePath}"" sha256_checksum=""{newChecksum}"" original_checksum=""{originalChecksum}"" />
    <content_on_disk>{SecurityElement.Escape(finalContent)}</content_on_disk>
</tool_response>";
    }

    private string CreatePatchFailureResponse(PatchResult result, string path)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Failed to apply patch to {path}");
        _ = notes.AppendLine($"Error: {result.Error}");

        if (result.ConflictingHunk != null)
        {
            _ = notes.AppendLine("Conflicting hunk details:");
            _ = notes.AppendLine($"  Original lines: {result.ConflictingHunk.OriginalStart}-{result.ConflictingHunk.OriginalStart + result.ConflictingHunk.OriginalLength - 1}");
        }

        return $@"<tool_response tool_name=""apply_code_patch"">
    <notes>
        {SecurityElement.Escape(notes.ToString().Trim())}
    </notes>
    <result status=""FAILED"" />
    <e>{SecurityElement.Escape(result.Error)}</e>
</tool_response>";
    }

    private string CreateErrorResponse(string toolName, string error)
    {
        return $@"<tool_response tool_name=""{toolName}"">
    <result status=""FAILED"" />
    <e>{SecurityElement.Escape(error)}</e>
</tool_response>";
    }

    private static string ComputeSha256(string s)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
