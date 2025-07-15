namespace Mogzi.Tools;

public class ReadTextFileTool(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ReadTextFile,
            new AIFunctionFactoryOptions
            {
                Name = "read_text_file",
                Description = "Reads and returns the content of a text file from the local filesystem. Supports reading specific line ranges for large files. Use this for source code, configuration files, documentation, and other text-based files."
            });
    }

    public async Task<string> ReadTextFile(
        [Description("The absolute path to the text file to read (e.g., '/home/user/project/file.txt'). Relative paths are not supported. You must provide an absolute path.")] string absolute_path,
        [Description("Optional: The 0-based line number to start reading from. Requires 'limit' to be set. Use for paginating through large files.")] int? offset = null,
        [Description("Optional: Maximum number of lines to read. Use with 'offset' to paginate through large files. If omitted, reads the entire file (if feasible, up to a default limit of 5000).")] int? limit = 5000)
    {
        limit ??= 5000;

        _llmResponseDetailsCallback?.Invoke($"Reading text file '{absolute_path}'{(offset.HasValue ? $" from line {offset}" : "")}{(limit.HasValue ? $" (limit: {limit} lines)" : "")}.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(absolute_path, offset, limit);
            if (validationError != null)
            {
                return CreateErrorResponse("read_text_file", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(absolute_path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("read_text_file", $"File path must be within the root directory ({workingDirectory}): {absolute_path}");
            }

            // Check if file exists
            if (!File.Exists(absolutePath))
            {
                return CreateErrorResponse("read_text_file", $"File not found: {absolute_path}");
            }

            // Check if file is readable
            var fileInfo = new FileInfo(absolutePath);
            if (!HasReadPermission(fileInfo))
            {
                return CreateErrorResponse("read_text_file", $"File is not readable: {absolute_path}");
            }

            // Read text file content
            string content;
            if (offset.HasValue)
            {
                content = await ReadFileWithRange(absolutePath, offset, limit);
            }
            else
            {
                content = await File.ReadAllTextAsync(absolutePath);
                if (content.Length > limit)
                {
                    content = content[..limit.Value];
                }
            }
            var checksum = ComputeSha256(content);
            var lineCount = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).Length;

            return CreateSuccessResponse(absolute_path, absolutePath, content, checksum, lineCount, offset, limit);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("read_text_file", $"Access denied reading file: {absolute_path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("read_text_file", $"Directory not found for file: {absolute_path}");
        }
        catch (FileNotFoundException)
        {
            return CreateErrorResponse("read_text_file", $"File not found: {absolute_path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("read_text_file", $"I/O error reading file: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error reading text file. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("read_text_file", $"Unexpected error: {ex.Message}");
        }
    }

    private string? ValidateParameters(string path, int? offset, int? limit)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Path cannot be empty or whitespace";
        }

        // Check if path is absolute
        if (!Path.IsPathRooted(path))
        {
            return $"File path must be absolute, but was relative: {path}. You must provide an absolute path.";
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            return "Path contains invalid characters";
        }

        // Validate offset and limit parameters
        if (offset.HasValue && offset.Value < 0)
        {
            return "Offset must be a non-negative number";
        }

        if (limit.HasValue && limit.Value <= 0)
        {
            return "Limit must be a positive number";
        }

        if (offset.HasValue && !limit.HasValue)
        {
            return "When offset is specified, limit must also be specified";
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

    private bool HasReadPermission(FileInfo fileInfo)
    {
        try
        {
            // Try to open the file for reading to check permissions
            using var stream = fileInfo.OpenRead();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> ReadFileWithRange(string absolutePath, int? offset, int? limit)
    {
        var lines = await File.ReadAllLinesAsync(absolutePath);

        var startLine = offset ?? 0;
        var endLine = limit.HasValue ? Math.Min(startLine + limit.Value, lines.Length) : lines.Length;

        if (startLine >= lines.Length)
        {
            return string.Empty;
        }

        var selectedLines = lines.Skip(startLine).Take(endLine - startLine);
        return string.Join(Environment.NewLine, selectedLines);
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, string content, string checksum, int lineCount, int? offset, int? limit)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Successfully read text file {relativePath}");
        _ = notes.AppendLine($"Total lines: {lineCount}");
        _ = notes.AppendLine($"Content size: {content.Length} characters");

        if (offset.HasValue || limit.HasValue)
        {
            _ = notes.AppendLine($"Range: lines {offset ?? 0} to {(offset ?? 0) + (limit ?? lineCount) - 1}");
        }

        return $@"<tool_response tool_name=""read_text_file"">
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
