using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace MaxBot.Tools;

public class WriteFileTool(MaxbotConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly MaxbotConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            WriteFile,
            new AIFunctionFactoryOptions
            {
                Name = "write_file",
                Description = "Writes content to a specified file in the local filesystem."
            });
    }

    public async Task<string> WriteFile(
        [Description("The absolute path to the file to write to (e.g., '/home/user/project/file.txt'). Relative paths are not supported.")] string file_path,
        [Description("The content to write to the file.")] string content)
    {
        _llmResponseDetailsCallback?.Invoke($"Writing to file '{file_path}'.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(file_path, content);
            if (validationError != null)
            {
                return CreateErrorResponse("write_file", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(file_path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("write_file", $"File path must be within the root directory ({workingDirectory}): {file_path}");
            }

            // Check if target is a directory
            if (Directory.Exists(absolutePath))
            {
                return CreateErrorResponse("write_file", $"Path is a directory, not a file: {file_path}");
            }

            // Check tool approvals for write operations
            if (_config.ToolApprovals == "readonly")
            {
                return CreateErrorResponse("write_file", "Tool approvals are set to readonly mode. File write operations are not permitted.");
            }

            // Check if file exists and is read-only
            if (File.Exists(absolutePath))
            {
                var fileInfo = new FileInfo(absolutePath);
                if (fileInfo.IsReadOnly)
                {
                    return CreateErrorResponse("write_file", $"File is read-only and cannot be written: {file_path}");
                }
            }

            // Create directory if it doesn't exist
            var directoryPath = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            // Read original content if file exists for diff generation
            var originalContent = "";
            var isNewFile = !File.Exists(absolutePath);
            if (!isNewFile)
            {
                try
                {
                    originalContent = await File.ReadAllTextAsync(absolutePath);
                }
                catch (Exception ex)
                {
                    return CreateErrorResponse("write_file", $"Error reading existing file for backup: {ex.Message}");
                }
            }

            // Write content to file
            await File.WriteAllTextAsync(absolutePath, content);

            // Verify the write operation by reading back the content
            string writtenContent;
            try
            {
                writtenContent = await File.ReadAllTextAsync(absolutePath);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("write_file", $"Error verifying written content: {ex.Message}");
            }

            // Verify content matches what we intended to write
            if (writtenContent != content)
            {
                return CreateErrorResponse("write_file", "Content verification failed - written content does not match intended content");
            }

            var checksum = ComputeSha256(writtenContent);
            var lineCount = writtenContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).Length;

            return CreateSuccessResponse(file_path, absolutePath, writtenContent, checksum, lineCount, isNewFile, originalContent);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("write_file", $"Access denied writing to file: {file_path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("write_file", $"Directory not found for file: {file_path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("write_file", $"I/O error writing file: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error writing file. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("write_file", $"Unexpected error: {ex.Message}");
        }
    }

    private string? ValidateParameters(string filePath, string content)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return "File path cannot be empty or whitespace";
        }

        if (content == null)
        {
            return "Content cannot be null";
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

    private string CreateSuccessResponse(string relativePath, string absolutePath, string content, string checksum, int lineCount, bool isNewFile, string originalContent)
    {
        var notes = new StringBuilder();
        _ = isNewFile
            ? notes.AppendLine($"Successfully created and wrote to new file: {relativePath}")
            : notes.AppendLine($"Successfully overwrote file: {relativePath}");
        _ = notes.AppendLine($"Total lines: {lineCount}");
        _ = notes.AppendLine($"Content size: {content.Length} characters");

        // Generate a simple diff summary for the notes
        if (!isNewFile && originalContent != content)
        {
            var originalLines = originalContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).Length;
            var newLines = lineCount;
            var lineDiff = newLines - originalLines;
            _ = lineDiff > 0
                ? notes.AppendLine($"Added {lineDiff} lines")
                : lineDiff < 0
                    ? notes.AppendLine($"Removed {Math.Abs(lineDiff)} lines")
                    : notes.AppendLine("Modified content (same line count)");
        }

        return $@"<tool_response tool_name=""write_file"">
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
