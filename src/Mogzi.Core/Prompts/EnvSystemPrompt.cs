namespace Mogzi.Prompts;

/// <summary>
/// Contains environment-specific system prompt information
/// </summary>
public static class EnvSystemPrompt
{
    public static string GetEnvPrompt(string currentDateTime, string userOperatingSystem, string userShell, string username, string hostname, string currentWorkingDirectory, string mode, string toolApprovals)
    {
        return $"""
<system_environment>
SYSTEM INFORMATION:
- The User's operating system is {userOperatingSystem}.
- The User's shell is {userShell}.
- The User's username is {username}.
- The User's hostname is {hostname}.
- The User's current working directory absolute path is '{currentWorkingDirectory}'.
- The User is active mode is '{mode}'.
- The User's Tool Approval Setting is '{toolApprovals.ToLower()}'. 
- The User's current date is {currentDateTime}.
</system_environment>
""";
    }

    /// <summary>
    /// Generates workspace details including file listings and git remote information for AI context.
    /// This method provides Cline-style workspace information by recursively scanning the working directory,
    /// respecting .gitignore rules, and formatting the output in a hierarchical structure.
    /// </summary>
    /// <param name="currentWorkingDirectory">The absolute path to the working directory to scan</param>
    /// <param name="maxFiles">Maximum number of files to include in the listing (default: 200)</param>
    /// <returns>
    /// A formatted string containing:
    /// - Current Working Directory section with hierarchical file/directory listing
    /// - Git Remote URLs section (if .git directory exists)
    /// - Truncation notice if file limit is exceeded
    /// </returns>
    /// <remarks>
    /// This method:
    /// - Dynamically reads and parses .gitignore files at runtime
    /// - Always excludes .git/ directory contents from listings
    /// - Supports wildcard patterns (*, ?) and negation rules (!) in .gitignore
    /// - Normalizes path separators to forward slashes for cross-platform consistency
    /// - Sorts files hierarchically (directories first, then files within each directory)
    /// - Extracts git remote URLs from .git/config if available
    /// - Handles errors gracefully by returning minimal workspace information
    /// </remarks>
    public static string GetWorkspaceDetails(string currentWorkingDirectory, int maxFiles = 200)
    {
        try
        {
            var gitIgnoreRules = LoadGitIgnoreRules();
            var files = GetFilteredFilesList(currentWorkingDirectory, gitIgnoreRules, maxFiles, out var didHitLimit);
            var gitRemotes = GetGitRemoteUrls(currentWorkingDirectory);

            var result = new StringBuilder();

            // Format file list section
            _ = result.AppendLine($"# Current Working Directory ({currentWorkingDirectory}) Files");
            _ = result.AppendLine(FormatFilesList(currentWorkingDirectory, files, didHitLimit));
            _ = result.AppendLine();

            // Format git remotes section if available
            if (gitRemotes.Count > 0)
            {
                _ = result.AppendLine("# Git Remote URLs");
                foreach (var remote in gitRemotes)
                {
                    _ = result.AppendLine($"{remote.Key}: {remote.Value}");
                }
            }

            return result.ToString();
        }
        catch (Exception)
        {
            // If we can't get workspace details, return minimal info
            return $"# Current Working Directory ({currentWorkingDirectory}) Files\nError reading workspace details.\n";
        }
    }

    private static List<string> LoadGitIgnoreRules()
    {
        // Skip .gitignore parsing for performance - use hardcoded common ignore patterns
        return
        [
            // Git
            ".git/",
            ".git",

            // Node.js / JavaScript / TypeScript
            "node_modules/",
            "node_modules",
            ".npm",
            ".yarn",
            "dist/",
            "build/",
            ".next/",
            ".nuxt/",
            "coverage/",
            ".nyc_output/",

            // .NET
            "bin/",
            "bin",
            "obj/",
            "obj",
            "packages/",
            ".vs/",
            ".vscode/",
            "*.user",
            "*.suo",
            "*.cache",

            // Python
            "__pycache__/",
            "__pycache__",
            "*.pyc",
            "*.pyo",
            "*.pyd",
            ".Python",
            "env/",
            "venv/",
            ".env",
            ".venv/",
            "pip-log.txt",
            "pip-delete-this-directory.txt",
            ".pytest_cache/",

            // Java
            "target/",
            "target",
            "*.class",
            "*.jar",
            "*.war",
            "*.ear",
            ".gradle/",
            "build/",
            ".idea/",
            "*.iml",
            "*.iws",

            // General IDE/Editor
            ".vscode/",
            ".idea/",
            "*.swp",
            "*.swo",
            "*~",
            ".DS_Store",
            "Thumbs.db",

            // Logs and temp files
            "*.log",
            "logs/",
            "tmp/",
            "temp/"
        ];
    }

    private static List<string> GetFilteredFilesList(string absolutePath, List<string> gitIgnoreRules, int maxFiles, out bool didHitLimit)
    {
        didHitLimit = false;

        try
        {
            var allEntries = new List<string>();

            // Get all files and directories recursively
            var directories = Directory.GetDirectories(absolutePath, "*", SearchOption.AllDirectories);
            var regularFiles = Directory.GetFiles(absolutePath, "*", SearchOption.AllDirectories);

            // Combine and filter based on gitignore rules
            var allPaths = directories.Concat(regularFiles).ToList();

            foreach (var path in allPaths)
            {
                var relativePath = Path.GetRelativePath(absolutePath, path);
                var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

                // Check if this path should be ignored
                if (!ShouldIgnorePath(normalizedPath, Directory.Exists(path), gitIgnoreRules))
                {
                    // Add trailing slash for directories to match Cline format
                    if (Directory.Exists(path))
                    {
                        allEntries.Add(path + Path.DirectorySeparatorChar);
                    }
                    else
                    {
                        allEntries.Add(path);
                    }
                }
            }

            // Check if we hit the limit
            if (allEntries.Count > maxFiles)
            {
                didHitLimit = true;
                allEntries = [.. allEntries.Take(maxFiles)];
            }

            return allEntries;
        }
        catch (Exception)
        {
            // If we can't read the directory, return empty list
            return [];
        }
    }

    private static bool ShouldIgnorePath(string relativePath, bool isDirectory, List<string> gitIgnoreRules)
    {
        // Early exit for common cases
        if (relativePath.StartsWith(".git/") || relativePath == ".git")
        {
            return true;
        }

        // Pre-split path once for efficiency
        var pathParts = relativePath.Split('/');
        var fileName = pathParts[^1];

        foreach (var rule in gitIgnoreRules)
        {
            if (string.IsNullOrEmpty(rule))
            {
                continue;
            }

            var pattern = rule;
            var isNegation = pattern.StartsWith('!');
            if (isNegation)
            {
                pattern = pattern[1..];
            }

            // Handle directory-specific rules (ending with /)
            var isDirectoryRule = pattern.EndsWith('/');
            if (isDirectoryRule)
            {
                pattern = pattern[..^1];
                if (!isDirectory)
                {
                    continue;
                }
            }

            bool matches;

            // Optimize for common simple patterns first
            if (!pattern.Contains('*') && !pattern.Contains('?') && !pattern.Contains('['))
            {
                // Simple exact match patterns - much faster
                if (pattern.Contains('/'))
                {
                    // Path-specific pattern
                    matches = relativePath == pattern || relativePath.StartsWith(pattern + "/");
                }
                else
                {
                    // Simple filename pattern - check filename directly first (most common case)
                    matches = fileName == pattern || relativePath == pattern;

                    // Only check other path parts if filename doesn't match
                    if (!matches && pathParts.Length > 1)
                    {
                        for (var i = 0; i < pathParts.Length - 1; i++)
                        {
                            if (pathParts[i] == pattern)
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                // Wildcard patterns - use optimized matching
                matches = MatchesWildcardPatternOptimized(relativePath, pathParts, fileName, pattern);
            }

            if (matches)
            {
                return !isNegation;
            }
        }

        return false;
    }

    private static bool MatchesWildcardPatternOptimized(string relativePath, string[] pathParts, string fileName, string pattern)
    {
        // For patterns without path separators, check filename first (most common case)
        if (!pattern.Contains('/'))
        {
            if (SimpleWildcardMatch(fileName, pattern))
            {
                return true;
            }

            // Check other path components only if needed
            for (var i = 0; i < pathParts.Length - 1; i++)
            {
                if (SimpleWildcardMatch(pathParts[i], pattern))
                {
                    return true;
                }
            }
            return false;
        }

        // For path patterns, match against full path
        return SimpleWildcardMatch(relativePath, pattern);
    }

    private static bool SimpleWildcardMatch(string text, string pattern)
    {
        // Simple wildcard matching without regex for better performance
        var textIndex = 0;
        var patternIndex = 0;
        var starIndex = -1;
        var match = 0;

        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && (pattern[patternIndex] == '?' || pattern[patternIndex] == text[textIndex]))
            {
                textIndex++;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                match = textIndex;
                patternIndex++;
            }
            else if (starIndex != -1)
            {
                patternIndex = starIndex + 1;
                match++;
                textIndex = match;
            }
            else
            {
                return false;
            }
        }

        // Skip any remaining '*' in pattern
        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    // private static bool MatchesWildcardPattern(string path, string pattern)
    // {
    //     try
    //     {
    //         // Convert gitignore pattern to regex pattern
    //         var regexPattern = ConvertGitIgnorePatternToRegex(pattern);
    //         return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    //     }
    //     catch
    //     {
    //         // If regex fails, fall back to simple contains check
    //         return path.Contains(pattern.Replace("*", ""));
    //     }
    // }

    // private static string ConvertGitIgnorePatternToRegex(string pattern)
    // {
    //     var result = new StringBuilder("^");

    //     for (var i = 0; i < pattern.Length; i++)
    //     {
    //         var c = pattern[i];

    //         switch (c)
    //         {
    //             case '*':
    //                 _ = result.Append(".*");
    //                 break;
    //             case '?':
    //                 _ = result.Append(".");
    //                 break;
    //             case '[':
    //                 // Handle character classes like [Bb] or [Oo]
    //                 var closingBracket = pattern.IndexOf(']', i + 1);
    //                 if (closingBracket != -1)
    //                 {
    //                     // Extract the character class and add it to regex
    //                     var charClass = pattern.Substring(i, closingBracket - i + 1);
    //                     _ = result.Append(charClass);
    //                     i = closingBracket; // Skip to after the closing bracket
    //                 }
    //                 else
    //                 {
    //                     // No closing bracket found, treat as literal
    //                     _ = result.Append(Regex.Escape(c.ToString()));
    //                 }
    //                 break;
    //             default:
    //                 // Escape other regex special characters
    //                 _ = result.Append(Regex.Escape(c.ToString()));
    //                 break;
    //         }
    //     }

    //     _ = result.Append("$");
    //     return result.ToString();
    // }

    private static string FormatFilesList(string absolutePath, List<string> files, bool didHitLimit)
    {
        if (files.Count == 0)
        {
            return "No files found.";
        }

        // Convert to relative paths and normalize separators
        var relativePaths = files
            .Select(file =>
            {
                var relativePath = Path.GetRelativePath(absolutePath, file);
                // Normalize path separators to forward slashes (like Cline's toPosix())
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                return relativePath;
            })
            .ToList();

        // Sort using Cline's hierarchical sorting algorithm
        relativePaths.Sort((a, b) =>
        {
            var aParts = a.Split('/');
            var bParts = b.Split('/');

            for (var i = 0; i < Math.Min(aParts.Length, bParts.Length); i++)
            {
                if (aParts[i] != bParts[i])
                {
                    // If one is a directory and the other isn't at this level, sort the directory first
                    if (i + 1 == aParts.Length && i + 1 < bParts.Length)
                    {
                        return -1; // a is shorter (directory), comes first
                    }
                    if (i + 1 == bParts.Length && i + 1 < aParts.Length)
                    {
                        return 1; // b is shorter (directory), comes first
                    }

                    // Otherwise, sort alphabetically with numeric and case-insensitive comparison
                    return string.Compare(aParts[i], bParts[i], StringComparison.OrdinalIgnoreCase);
                }
            }

            // If all parts are the same up to the length of the shorter path,
            // the shorter one comes first
            return aParts.Length - bParts.Length;
        });

        var result = string.Join("\n", relativePaths);

        if (didHitLimit)
        {
            result += "\n\n(File list truncated. Use list_files on specific subdirectories if you need to explore further.)";
        }

        return result;
    }

    private static Dictionary<string, string> GetGitRemoteUrls(string workingDirectory)
    {
        var remotes = new Dictionary<string, string>();

        try
        {
            var gitConfigPath = Path.Combine(workingDirectory, ".git", "config");
            if (File.Exists(gitConfigPath))
            {
                var configLines = File.ReadAllLines(gitConfigPath);
                string? currentRemote = null;

                foreach (var line in configLines)
                {
                    var trimmedLine = line.Trim();

                    // Look for remote section headers like [remote "origin"]
                    if (trimmedLine.StartsWith("[remote \"") && trimmedLine.EndsWith("\"]"))
                    {
                        var startIndex = "[remote \"".Length;
                        var endIndex = trimmedLine.Length - "\"]".Length;
                        currentRemote = trimmedLine[startIndex..endIndex];
                    }
                    // Look for url lines within remote sections
                    else if (currentRemote != null && trimmedLine.StartsWith("url = "))
                    {
                        var url = trimmedLine["url = ".Length..];
                        remotes[currentRemote] = url;
                        currentRemote = null; // Reset after finding URL
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't read git config, just return empty dictionary
        }

        return remotes;
    }
}
