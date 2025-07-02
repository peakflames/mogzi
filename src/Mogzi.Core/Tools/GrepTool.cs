using System.ComponentModel;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Mogzi.Tools;

public class GrepTool(ApplicationConfiguration config, Action<string, ConsoleColor>? llmResponseDetailsCallback = null, IWorkingDirectoryProvider? workingDirectoryProvider = null)
{
    private readonly ApplicationConfiguration _config = config;
    private readonly Action<string, ConsoleColor>? _llmResponseDetailsCallback = llmResponseDetailsCallback;
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider ?? new DefaultWorkingDirectoryProvider();

    public AIFunction GetTool()
    {
        return AIFunctionFactory.Create(
            SearchFileContent,
            new AIFunctionFactoryOptions
            {
                Name = "search_file_content",
                Description = "Searches for a regular expression pattern within the content of files in a specified directory (or current working directory). Can filter files by a glob pattern. Returns the lines containing matches, along with their file paths and line numbers."
            });
    }

    public async Task<string> SearchFileContent(
        [Description("The regular expression (regex) pattern to search for within file contents (e.g., 'function\\s+myFunction', 'import\\s+\\{.*\\}\\s+from\\s+.*').")] string pattern,
        [Description("Optional: The absolute path to the directory to search within. If omitted, searches the current working directory.")] string? path = null,
        [Description("Optional: A glob pattern to filter which files are searched (e.g., '*.js', '*.{ts,tsx}', 'src/**'). If omitted, searches all files (respecting potential global ignores).")] string? include = null)
    {
        _llmResponseDetailsCallback?.Invoke($"Searching for pattern '{pattern}'{(path != null ? $" in {path}" : "")}{(include != null ? $" (filter: {include})" : "")}.", ConsoleColor.DarkGray);

        try
        {
            // Validate parameters
            var validationError = ValidateParameters(pattern, path);
            if (validationError != null)
            {
                return CreateErrorResponse("search_file_content", validationError);
            }

            var workingDirectory = _workingDirectoryProvider.GetCurrentDirectory();
            var searchDirectory = path != null ? Path.GetFullPath(path) : workingDirectory;

            // Security validation - ensure path is within working directory
            if (!IsPathInWorkingDirectory(searchDirectory, workingDirectory))
            {
                return CreateErrorResponse("search_file_content", $"Search path must be within the root directory ({workingDirectory}): {path}");
            }

            // Check if directory exists
            if (!Directory.Exists(searchDirectory))
            {
                return CreateErrorResponse("search_file_content", $"Directory not found: {searchDirectory}");
            }

            // Perform the search
            var matches = await PerformGrepSearch(pattern, searchDirectory, include);

            if (matches.Count == 0)
            {
                var searchDirDisplay = path ?? ".";
                var noMatchMsg = $"No matches found for pattern \"{pattern}\" in path \"{searchDirDisplay}\"{(include != null ? $" (filter: \"{include}\")" : "")}.";
                return CreateSuccessResponse("search_file_content", searchDirectory, noMatchMsg, matches);
            }

            // Group matches by file
            var matchesByFile = matches
                .GroupBy(m => m.FilePath)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(m => m.LineNumber).ToList());

            var searchDirDisplayName = path ?? ".";
            var resultContent = new StringBuilder();
            _ = resultContent.AppendLine($"Found {matches.Count} match(es) for pattern \"{pattern}\" in path \"{searchDirDisplayName}\"{(include != null ? $" (filter: \"{include}\")" : "")}:");
            _ = resultContent.AppendLine("---");

            foreach (var fileGroup in matchesByFile)
            {
                var relativeFilePath = Path.GetRelativePath(searchDirectory, fileGroup.Key);
                if (relativeFilePath == ".")
                {
                    relativeFilePath = Path.GetFileName(fileGroup.Key);
                }

                _ = resultContent.AppendLine($"File: {relativeFilePath}");
                foreach (var match in fileGroup.Value)
                {
                    var trimmedLine = match.Line.Trim();
                    _ = resultContent.AppendLine($"L{match.LineNumber}: {trimmedLine}");
                }
                _ = resultContent.AppendLine("---");
            }

            return CreateSuccessResponse("search_file_content", searchDirectory, resultContent.ToString().Trim(), matches);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateErrorResponse("search_file_content", $"Access denied searching in directory: {path ?? "current directory"}");
        }
        catch (DirectoryNotFoundException)
        {
            return CreateErrorResponse("search_file_content", $"Directory not found: {path ?? "current directory"}");
        }
        catch (IOException ex)
        {
            return CreateErrorResponse("search_file_content", $"I/O error during search: {ex.Message}");
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"ERROR: Error during grep search. {ex.Message}", ConsoleColor.Red);
            }
            return CreateErrorResponse("search_file_content", $"Unexpected error: {ex.Message}");
        }
    }

    private string? ValidateParameters(string pattern, string? path)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return "Pattern cannot be empty or whitespace";
        }

        // Validate regex pattern
        try
        {
            _ = new Regex(pattern);
        }
        catch (ArgumentException ex)
        {
            return $"Invalid regular expression pattern provided: {pattern}. Error: {ex.Message}";
        }

        // Validate path if provided
        if (path != null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "Path cannot be empty or whitespace when specified";
            }

            // Check if path is absolute (matching TypeScript implementation)
            if (!Path.IsPathRooted(path))
            {
                return $"Path must be absolute, but was relative: {path}. You must provide an absolute path.";
            }

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            if (path.Any(c => invalidChars.Contains(c)))
            {
                return "Path contains invalid characters";
            }
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

    private async Task<List<GrepMatch>> PerformGrepSearch(string pattern, string searchDirectory, string? include)
    {
        var matches = new List<GrepMatch>();

        // Try git grep first if we're in a git repository
        if (await TryGitGrep(pattern, searchDirectory, include, matches))
        {
            return matches;
        }

        // Try system grep if available
        if (await TrySystemGrep(pattern, searchDirectory, include, matches))
        {
            return matches;
        }

        // Fallback to JavaScript-style implementation
        return await JavaScriptFallbackGrep(pattern, searchDirectory, include);
    }

    private async Task<bool> TryGitGrep(string pattern, string searchDirectory, string? include, List<GrepMatch> matches)
    {
        try
        {
            if (!IsGitRepository(searchDirectory) || !await IsCommandAvailable("git"))
            {
                return false;
            }

            var gitArgs = new List<string> { "grep", "--untracked", "-n", "-E", "--ignore-case", pattern };
            if (include != null)
            {
                gitArgs.AddRange(["--", include]);
            }

            var output = await RunCommand("git", gitArgs, searchDirectory);
            if (output != null)
            {
                ParseGrepOutput(output, searchDirectory, matches);
                return true;
            }
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"Git grep failed: {ex.Message}. Falling back...", ConsoleColor.DarkGray);
            }
        }

        return false;
    }

    private async Task<bool> TrySystemGrep(string pattern, string searchDirectory, string? include, List<GrepMatch> matches)
    {
        try
        {
            if (!await IsCommandAvailable("grep"))
            {
                return false;
            }

            var grepArgs = new List<string> { "-r", "-n", "-H", "-E" };
            var commonExcludes = new[] { ".git", "node_modules", "bower_components" };
            foreach (var dir in commonExcludes)
            {
                grepArgs.Add($"--exclude-dir={dir}");
            }

            if (include != null)
            {
                grepArgs.Add($"--include={include}");
            }

            grepArgs.Add(pattern);
            grepArgs.Add(".");

            var output = await RunCommand("grep", grepArgs, searchDirectory);
            if (output != null)
            {
                ParseGrepOutput(output, searchDirectory, matches);
                return true;
            }
        }
        catch (Exception ex)
        {
            if (_config.Debug)
            {
                _llmResponseDetailsCallback?.Invoke($"System grep failed: {ex.Message}. Falling back...", ConsoleColor.DarkGray);
            }
        }

        return false;
    }

    private async Task<List<GrepMatch>> JavaScriptFallbackGrep(string pattern, string searchDirectory, string? include)
    {
        var matches = new List<GrepMatch>();
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        // Get all files in directory recursively
        var files = GetFilesRecursively(searchDirectory, include);

        foreach (var filePath in files)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        matches.Add(new GrepMatch
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            Line = lines[i]
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignore files that can't be read (binary files, permission issues, etc.)
                if (_config.Debug)
                {
                    _llmResponseDetailsCallback?.Invoke($"Could not read file {filePath}: {ex.Message}", ConsoleColor.DarkGray);
                }
            }
        }

        return matches;
    }

    private IEnumerable<string> GetFilesRecursively(string directory, string? include)
    {
        var excludeDirs = new HashSet<string> { ".git", "node_modules", "bower_components", ".svn", ".hg" };

        return GetFilesRecursivelyInternal(directory, include, excludeDirs);
    }

    private IEnumerable<string> GetFilesRecursivelyInternal(string directory, string? include, HashSet<string> excludeDirs)
    {
        var dirInfo = new DirectoryInfo(directory);

        // Skip excluded directories
        if (excludeDirs.Contains(dirInfo.Name))
        {
            yield break;
        }

        // Get files in current directory
        FileInfo[] files;
        try
        {
            files = dirInfo.GetFiles();
        }
        catch (UnauthorizedAccessException)
        {
            yield break; // Skip directories we can't access
        }
        catch (DirectoryNotFoundException)
        {
            yield break; // Skip directories that don't exist
        }

        foreach (var file in files)
        {
            if (include == null || MatchesGlobPattern(file.Name, include))
            {
                yield return file.FullName;
            }
        }

        // Recursively process subdirectories
        DirectoryInfo[] subDirs;
        try
        {
            subDirs = dirInfo.GetDirectories();
        }
        catch (UnauthorizedAccessException)
        {
            yield break; // Skip directories we can't access
        }
        catch (DirectoryNotFoundException)
        {
            yield break; // Skip directories that don't exist
        }

        foreach (var subDir in subDirs)
        {
            foreach (var file in GetFilesRecursivelyInternal(subDir.FullName, include, excludeDirs))
            {
                yield return file;
            }
        }
    }

    private bool MatchesGlobPattern(string fileName, string pattern)
    {
        // Simple glob pattern matching - convert to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }

    private bool IsGitRepository(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            {
                return true;
            }
            current = current.Parent;
        }
        return false;
    }

    private async Task<bool> IsCommandAvailable(string command)
    {
        try
        {
            var checkCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "command";
            var checkArgs = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new[] { command } : ["-v", command];

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = checkCommand,
                    Arguments = string.Join(" ", checkArgs),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _ = process.Start();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> RunCommand(string command, List<string> args, string workingDirectory)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _ = process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return output;
            }
            else if (process.ExitCode == 1)
            {
                return ""; // No matches found
            }
            else
            {
                throw new Exception($"{command} exited with code {process.ExitCode}: {error}");
            }
        }
        catch
        {
            return null;
        }
    }

    private void ParseGrepOutput(string output, string basePath, List<GrepMatch> matches)
    {
        if (string.IsNullOrEmpty(output))
        {
            return;
        }

        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Find the first colon
            var firstColonIndex = line.IndexOf(':');
            if (firstColonIndex == -1)
            {
                continue;
            }

            // Find the second colon
            var secondColonIndex = line.IndexOf(':', firstColonIndex + 1);
            if (secondColonIndex == -1)
            {
                continue;
            }

            var filePathRaw = line[..firstColonIndex];
            var lineNumberStr = line.Substring(firstColonIndex + 1, secondColonIndex - firstColonIndex - 1);
            var lineContent = line[(secondColonIndex + 1)..];

            if (int.TryParse(lineNumberStr, out var lineNumber))
            {
                var absoluteFilePath = Path.IsPathRooted(filePathRaw)
                    ? filePathRaw
                    : Path.Combine(basePath, filePathRaw);

                matches.Add(new GrepMatch
                {
                    FilePath = absoluteFilePath,
                    LineNumber = lineNumber,
                    Line = lineContent
                });
            }
        }
    }

    private string CreateSuccessResponse(string toolName, string searchDirectory, string content, List<GrepMatch> matches)
    {
        var notes = new StringBuilder();
        _ = notes.AppendLine($"Successfully searched for pattern in {searchDirectory}");
        _ = notes.AppendLine($"Total matches found: {matches.Count}");
        _ = notes.AppendLine($"Files searched: {matches.Select(m => m.FilePath).Distinct().Count()}");

        var checksum = ComputeSha256(content);

        return $@"<tool_response tool_name=""{toolName}"">
    <notes>{SecurityElement.Escape(notes.ToString().Trim())}</notes>
    <result status=""SUCCESS"" absolute_path=""{SecurityElement.Escape(searchDirectory)}"" sha256_checksum=""{checksum}"" />
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

    private class GrepMatch
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Line { get; set; } = string.Empty;
    }
}
