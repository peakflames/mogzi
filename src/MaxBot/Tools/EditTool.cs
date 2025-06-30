using Microsoft.Extensions.AI;
using MaxBot.Domain;
using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace MaxBot.Tools;

public class EditTool
{
    private readonly MaxbotConfiguration _config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = null;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

    public EditTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
    {
        _config = config;
        _llmResponseDetailsCallback = llmResponseDetailsCallback;
        _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();
    }

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            Replace,
            new AIFunctionFactoryOptions
            {
                Name = "replace",
                Description = "Replaces text within a file. By default, replaces a single occurrence, but can replace multiple occurrences when `expected_replacements` is specified. This tool requires providing significant context around the change to ensure precise targeting. Always use the read_file tool to examine the file's current content before attempting a text replacement.\n\nExpectation for required parameters:\n1. `file_path` MUST be an absolute path; otherwise an error will be thrown.\n2. `old_string` MUST be the exact literal text to replace (including all whitespace, indentation, newlines, and surrounding code etc.).\n3. `new_string` MUST be the exact literal text to replace `old_string` with (also including all whitespace, indentation, newlines, and surrounding code etc.). Ensure the resulting code is correct and idiomatic.\n4. NEVER escape `old_string` or `new_string`, that would break the exact literal text requirement.\n**Important:** If ANY of the above are not satisfied, the tool will fail. CRITICAL for `old_string`: Must uniquely identify the single instance to change. Include at least 3 lines of context BEFORE and AFTER the target text, matching whitespace and indentation precisely. If this string matches multiple locations, or does not match exactly, the tool will fail.\n**Multiple replacements:** Set `expected_replacements` to the number of occurrences you want to replace. The tool will replace ALL occurrences that match `old_string` exactly. Ensure the number of replacements matches your expectation."
            });
    }

    public async Task<string> Replace(
        [Description("The absolute path to the file to modify. Must start with '/'.")]
        string file_path,
        [Description("The exact literal text to replace, preferably unescaped. For single replacements (default), include at least 3 lines of context BEFORE and AFTER the target text, matching whitespace and indentation precisely. For multiple replacements, specify expected_replacements parameter. If this string is not the exact literal text (i.e. you escaped it) or does not match exactly, the tool will fail.")]
        string old_string,
        [Description("The exact literal text to replace `old_string` with, preferably unescaped. Provide the EXACT text. Ensure the resulting code is correct and idiomatic.")]
        string new_string,
        [Description("Number of replacements expected. Defaults to 1 if not specified. Use when you want to replace multiple occurrences.")]
        int? expected_replacements = 1)
    {
        var expectedReplacements = expected_replacements ?? 1;
        
        _llmResponseDetailsCallback?.Invoke($"Replacing text in file '{file_path}' (expecting {expectedReplacements} replacement{(expectedReplacements == 1 ? "" : "s")}).", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(file_path, old_string, new_string, expectedReplacements);
            if (validationError != null)
            {
                return CreateErrorResponse("replace", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(file_path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("replace", $"File path must be within the root directory ({workingDirectory}): {file_path}");
            }

            // Check tool approvals for write operations
            if (_config.ToolApprovals == "readonly")
            {
                return CreateErrorResponse("replace", "Tool approvals are set to readonly mode. File edit operations are not permitted.");
            }

            // Handle file creation case (old_string is empty)
            if (string.IsNullOrEmpty(old_string))
            {
                return await HandleFileCreation(absolutePath, new_string);
            }

            // Check if file exists for editing
            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("replace", $"File not found. Cannot apply edit. Use an empty old_string to create a new file: {file_path}");
            }

            // Check if file is writable
            var fileInfo = new FileInfo(absolutePath);
            if (fileInfo.IsReadOnly)
            {
                return CreateErrorResponse("replace", $"File is read-only and cannot be modified: {file_path}");
            }

            // Read current content
            var currentContent = await File.ReadAllTextAsync(absolutePath);
            
            // Normalize line endings to LF for consistent processing
            currentContent = currentContent.Replace("\r\n", "\n");

            // Count occurrences of old_string
            var occurrences = CountOccurrences(currentContent, old_string);

            if (occurrences == 0)
            {
                return CreateErrorResponse("replace", $"Failed to edit, could not find the string to replace. Failed to edit, 0 occurrences found for old_string in {file_path}. No edits made. The exact text in old_string was not found. Ensure you're not escaping content incorrectly and check whitespace, indentation, and context. Use read_file tool to verify.");
            }

            if (occurrences != expectedReplacements)
            {
                return CreateErrorResponse("replace", $"Failed to edit, expected {expectedReplacements} occurrence(s) but found {occurrences}. Expected {expectedReplacements} occurrences but found {occurrences} for old_string in file: {file_path}");
            }

            // Perform the replacement
            var newContent = currentContent.Replace(old_string, new_string);

            // Create backup of original content
            var backupPath = $"{absolutePath}.backup.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            await File.WriteAllTextAsync(backupPath, currentContent);

            try
            {
                // Write new content to temporary file first
                var tempPath = $"{absolutePath}.tmp.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                await File.WriteAllTextAsync(tempPath, newContent);

                // Verify the temporary file was written correctly
                var verifyContent = await File.ReadAllTextAsync(tempPath);
                if (verifyContent != newContent)
                {
                    File.Delete(tempPath);
                    return CreateErrorResponse("replace", "Content verification failed during write operation");
                }

                // Atomic move from temp to target
                File.Move(tempPath, absolutePath, true);

                // Final verification
                var finalContent = await File.ReadAllTextAsync(absolutePath);
                if (finalContent != newContent)
                {
                    // Restore from backup
                    File.Move(backupPath, absolutePath, true);
                    return CreateErrorResponse("replace", "Final content verification failed - restored from backup");
                }

                // Clean up backup file on success
                File.Delete(backupPath);

                var checksum = ComputeSha256(finalContent);
                var lineCount = finalContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

                return CreateSuccessResponse(file_path, absolutePath, finalContent, checksum, lineCount, occurrences);
            }
            catch (Exception ex)
            {
                // Restore from backup if something went wrong
                if (File.Exists(backupPath))
                {
                    try
                    {
                        File.Move(backupPath, absolutePath, true);
                    }
                    catch
                    {
                        // Backup restoration failed - log but don't throw
                        _llmResponseDetailsCallback?.Invoke($"WARNING: Failed to restore backup after error: {ex.Message}", ConsoleColor.Yellow);
                    }
                }
                throw;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("replace", $"Access denied modifying file: {file_path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("replace", $"Directory not found for file: {file_path}");
        }
        catch (FileNotFoundException)
        {
            return CreateErrorResponse("replace", $"File not found: {file_path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("replace", $"I/O error modifying file: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error replacing text in file. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("replace", $"Unexpected error: {ex.Message}");
        }
    }

    private async Task<string> HandleFileCreation(string absolutePath, string newString)
    {
        try
        {
            // Check if file already exists
            if (File.Exists(absolutePath))
            {
                return CreateErrorResponse("replace", $"Failed to edit. Attempted to create a file that already exists. File already exists, cannot create: {absolutePath}");
            }

            // Create directory if it doesn't exist
            var directoryPath = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Write content to new file
            await File.WriteAllTextAsync(absolutePath, newString);

            // Verify the write operation
            var writtenContent = await File.ReadAllTextAsync(absolutePath);
            if (writtenContent != newString)
            {
                return CreateErrorResponse("replace", "Content verification failed during file creation");
            }

            var checksum = ComputeSha256(writtenContent);
            var lineCount = writtenContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;

            return CreateFileCreationResponse(absolutePath, writtenContent, checksum, lineCount);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse("replace", $"Error creating new file: {ex.Message}");
        }
    }

    private int CountOccurrences(string content, string searchString)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while ((index = content.IndexOf(searchString, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += searchString.Length;
        }
        return count;
    }

    private string? ValidateParameters(string filePath, string oldString, string newString, int expectedReplacements)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "File path cannot be empty or whitespace";
        }

        // Check if path is absolute (matching TypeScript implementation)
        if (!Path.IsPathRooted(filePath))
        {
            return $"File path must be absolute: {filePath}";
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (filePath.Any(c => invalidChars.Contains(c)))
        {
            return "File path contains invalid characters";
        }

        if (oldString == null)
        {
            return "old_string cannot be null";
        }

        if (newString == null)
        {
            return "new_string cannot be null";
        }

        if (expectedReplacements < 1)
        {
            return "expected_replacements must be at least 1";
        }

        return null;
    }

    private bool IsPathInWorkingDirectory(string absolutePath, string workingDirectory)
    {
        try
        {
            var normalizedAbsolutePath = Path.GetFullPath(absolutePath);
            var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);

            // Check if the path is exactly the working directory
            if (string.Equals(normalizedAbsolutePath, normalizedWorkingDirectory, 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return true;
            }

            // Ensure working directory ends with directory separator for subdirectory comparison
            if (!normalizedWorkingDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()) &&
                !normalizedWorkingDirectory.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                normalizedWorkingDirectory += Path.DirectorySeparatorChar;
            }

            return normalizedAbsolutePath.StartsWith(normalizedWorkingDirectory, 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, string content, string checksum, int lineCount, int replacements)
    {
        var notes = new StringBuilder();
        notes.AppendLine($"Successfully modified file: {relativePath} ({replacements} replacement{(replacements == 1 ? "" : "s")})");
        notes.AppendLine($"Total lines: {lineCount}");
        notes.AppendLine($"Content size: {content.Length} characters");

        return $@"<tool_response tool_name=""replace"">
    <notes>{SecurityElement.Escape(notes.ToString().Trim())}</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(absolutePath)}"" sha256_checksum=""{checksum}"" />
    <content_on_disk>{SecurityElement.Escape(content)}</content_on_disk>
</tool_response>";
    }

    private string CreateFileCreationResponse(string absolutePath, string content, string checksum, int lineCount)
    {
        var notes = new StringBuilder();
        notes.AppendLine($"Created new file: {absolutePath} with provided content");
        notes.AppendLine($"Total lines: {lineCount}");
        notes.AppendLine($"Content size: {content.Length} characters");

        return $@"<tool_response tool_name=""replace"">
    <notes>{SecurityElement.Escape(notes.ToString().Trim())}</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(absolutePath)}"" sha256_checksum=""{checksum}"" />
    <content_on_disk>{SecurityElement.Escape(content)}</content_on_disk>
</tool_response>";
    }

    private string CreateErrorResponse(string toolName, string error)
    {
        return $@"<tool_response tool_name=""{toolName}"">
    <result status=""FAILED"" />
    <error>{SecurityElement.Escape(error)}</error>
</tool_response>";
    }

    private static string ComputeSha256(string content)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
