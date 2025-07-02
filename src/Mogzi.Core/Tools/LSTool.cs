using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Mogzi.Tools;

public class LSTool(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            ListDirectory,
            new AIFunctionFactoryOptions
            {
                Name = "list_directory",
                Description = "Lists the names of files and subdirectories directly within a specified directory path. Can optionally ignore entries matching provided glob patterns."
            });
    }

    public string ListDirectory(
        [Description("The absolute path to the directory to list (must be absolute, not relative)")] string path,
        [Description("List of glob patterns to ignore")] string? ignore = null,
        [Description("Optional: Whether to respect .gitignore patterns when listing files. Only available in git repositories. Defaults to true.")] bool? respect_git_ignore = true)
    {
        _llmResponseDetailsCallback?.Invoke($"Listing directory '{path}'{(ignore?.Length > 0 ? $" with ignore patterns: {string.Join(", ", ignore)}" : "")}{(respect_git_ignore == false ? " (ignoring .gitignore)" : "")}.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(path);
            if (validationError != null)
            {
                return CreateErrorResponse("list_directory", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var absolutePath = Path.GetFullPath(path);

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(absolutePath, workingDirectory))
            {
                return CreateErrorResponse("list_directory", $"Path must be within the root directory ({workingDirectory}): {path}");
            }

            // Check if directory exists
            if (!Directory.Exists(absolutePath))
            {
                return CreateErrorResponse("list_directory", $"Directory not found: {path}");
            }

            // Check if directory is accessible
            if (!HasReadPermission(absolutePath))
            {
                return CreateErrorResponse("list_directory", $"Directory is not accessible: {path}");
            }

            // Parse ignore patterns from comma-separated string
            var ignorePatterns = ParseIgnorePatterns(ignore);

            // Get directory entries
            var entries = GetDirectoryEntries(absolutePath, ignorePatterns, respect_git_ignore ?? true, workingDirectory);

            if (entries.Count == 0)
            {
                return CreateEmptyDirectoryResponse(path, absolutePath);
            }

            // Sort entries (directories first, then alphabetically)
            entries.Sort((a, b) =>
            {
                if (a.IsDirectory && !b.IsDirectory)
                {
                    return -1;
                }

                if (!a.IsDirectory && b.IsDirectory)
                {
                    return 1;
                }

                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            var checksum = ComputeDirectoryChecksum(entries);
            return CreateSuccessResponse(path, absolutePath, entries, checksum);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("list_directory", $"Access denied to directory: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("list_directory", $"Directory not found: {path}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("list_directory", $"I/O error accessing directory: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error listing directory. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("list_directory", $"Unexpected error: {ex.Message}");
        }
    }

    private string[]? ParseIgnorePatterns(string? ignore)
    {
        if (string.IsNullOrWhiteSpace(ignore))
        {
            return null;
        }

        return [.. ignore.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pattern => pattern.Trim())
                    .Where(pattern => !string.IsNullOrWhiteSpace(pattern))];
    }

    private string? ValidateParameters(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Path cannot be empty or whitespace";
        }

        // Check if path is absolute (matching TypeScript implementation)
        if (!Path.IsPathRooted(path))
        {
            return $"Path must be absolute: {path}";
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            return "Path contains invalid characters";
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

    private bool HasReadPermission(string directoryPath)
    {
        try
        {
            // Try to enumerate the directory to check permissions
            _ = Directory.EnumerateFileSystemEntries(directoryPath).Take(1).ToList();
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

    private List<DirectoryEntry> GetDirectoryEntries(string absolutePath, string[]? ignorePatterns, bool respectGitIgnore, string workingDirectory)
    {
        var entries = new List<DirectoryEntry>();
        var gitIgnoredCount = 0;

        try
        {
            var fileSystemEntries = Directory.GetFileSystemEntries(absolutePath);

            foreach (var entry in fileSystemEntries)
            {
                var entryName = Path.GetFileName(entry);

                // Skip if matches ignore patterns
                if (ShouldIgnore(entryName, ignorePatterns))
                {
                    continue;
                }

                // Check .gitignore if enabled
                if (respectGitIgnore && ShouldGitIgnore(entry, workingDirectory))
                {
                    gitIgnoredCount++;
                    continue;
                }

                try
                {
                    var isDirectory = Directory.Exists(entry);
                    var size = 0L;
                    var modifiedTime = DateTime.MinValue;

                    if (isDirectory)
                    {
                        var dirInfo = new DirectoryInfo(entry);
                        modifiedTime = dirInfo.LastWriteTime;
                    }
                    else
                    {
                        var fileInfo = new FileInfo(entry);
                        size = fileInfo.Length;
                        modifiedTime = fileInfo.LastWriteTime;
                    }

                    entries.Add(new DirectoryEntry
                    {
                        Name = entryName,
                        Path = entry,
                        IsDirectory = isDirectory,
                        Size = size,
                        ModifiedTime = modifiedTime
                    });
                }
                catch (Exception ex)
                {
                    // Log error but continue with other entries
                    if (_config.Debug)
                    {
                        _llmResponseDetailsCallback?.Invoke($"Error accessing {entry}: {ex.Message}", ConsoleColor.DarkYellow);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"Error enumerating directory: {ex.Message}", ConsoleColor.Red);
            }
            throw;
        }

        return entries;
    }

    private bool ShouldIgnore(string fileName, string[]? patterns)
    {
        if (patterns == null || patterns.Length == 0)
        {
            return false;
        }

        foreach (var pattern in patterns)
        {
            if (MatchesGlobPattern(fileName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private bool MatchesGlobPattern(string fileName, string pattern)
    {
        try
        {
            // Convert glob pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace(@"\*", ".*")
                .Replace(@"\?", ".") + "$";

            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            // If regex fails, fall back to simple string comparison
            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    private bool ShouldGitIgnore(string filePath, string workingDirectory)
    {
        // Simple .gitignore implementation - check for common patterns
        // This is a basic implementation; a full implementation would parse .gitignore files
        try
        {
            var relativePath = Path.GetRelativePath(workingDirectory, filePath);
            var fileName = Path.GetFileName(filePath);

            // Common patterns to ignore
            var commonIgnorePatterns = new[]
            {
                ".git",
                "node_modules",
                "bin",
                "obj",
                ".vs",
                ".vscode",
                "*.tmp",
                "*.log",
                ".DS_Store",
                "Thumbs.db"
            };

            foreach (var pattern in commonIgnorePatterns)
            {
                if (pattern.StartsWith("*."))
                {
                    var extension = pattern[1..];
                    if (fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                         relativePath.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Check for actual .gitignore file if it exists
            var gitIgnorePath = Path.Combine(workingDirectory, ".gitignore");
            if (File.Exists(gitIgnorePath))
            {
                // This is a simplified check - a full implementation would properly parse .gitignore syntax
                var gitIgnoreContent = File.ReadAllText(gitIgnorePath);
                var lines = gitIgnoreContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    if (MatchesGlobPattern(fileName, trimmedLine) ||
                        MatchesGlobPattern(relativePath.Replace('\\', '/'), trimmedLine))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // If gitignore checking fails, don't ignore the file
        }

        return false;
    }

    private string CreateSuccessResponse(string relativePath, string absolutePath, List<DirectoryEntry> entries, string checksum)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Successfully listed directory {relativePath}");
        _ = notes.AppendLine($"Found {entries.Count} entries");

        var directoryCount = entries.Count(e => e.IsDirectory);
        var fileCount = entries.Count - directoryCount;
        _ = notes.AppendLine($"Directories: {directoryCount}, Files: {fileCount}");

        // Create directory listing content
        var content = new StringBuilder();
        _ = content.AppendLine($"Directory listing for {relativePath}:");

        foreach (var entry in entries)
        {
            var prefix = entry.IsDirectory ? "[DIR] " : "";
            var sizeInfo = entry.IsDirectory ? "" : $" ({FormatFileSize(entry.Size)})";
            _ = content.AppendLine($"{prefix}{entry.Name}{sizeInfo}");
        }

        return $@"<tool_response tool_name=""list_directory"">
    <notes>{SecurityElement.Escape(notes.ToString().Trim())}</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(absolutePath)}"" sha256_checksum=""{checksum}"" />
    <content_on_disk>{SecurityElement.Escape(content.ToString())}</content_on_disk>
</tool_response>";
    }

    private string CreateEmptyDirectoryResponse(string relativePath, string absolutePath)
    {
        var notes = $"Directory {relativePath} is empty";
        var content = $"Directory listing for {relativePath}:\n(empty directory)";
        var checksum = ComputeSha256(content);

        return $@"<tool_response tool_name=""list_directory"">
    <notes>{SecurityElement.Escape(notes)}</notes>
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

    private string ComputeDirectoryChecksum(List<DirectoryEntry> entries)
    {
        var content = string.Join("\n", entries.Select(e => $"{(e.IsDirectory ? "DIR" : "FILE")}:{e.Name}:{e.Size}:{e.ModifiedTime:yyyy-MM-dd HH:mm:ss}"));
        return ComputeSha256(content);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes == 0)
        {
            return "0 B";
        }

        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        var order = 0;
        var size = (double)bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private class DirectoryEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime ModifiedTime { get; set; }
    }
}
