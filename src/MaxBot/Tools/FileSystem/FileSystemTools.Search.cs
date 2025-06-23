using System.ComponentModel;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Text;
using System.Security;

namespace MaxBot.Tools;

public partial class FileSystemTools
{
    public string SearchFiles(
        [Description("The path of the directory to search in (relative to the current working directory). This directory will be recursively searched.")]
        string path,
        [Description("The regular expression pattern to search for. Uses standard .NET regex syntax.")]
        string regex,
        [Description("(optional) Glob pattern to filter files (e.g., '*.cs', 'src/**/*.js'). If not provided, it will search all files.")]
        string? file_pattern = null)
    {
        _llmResponseDetailsCallback?.Invoke($"Searching files in '{path}' with regex '{regex}' and pattern '{file_pattern ?? "*"}'.", ConsoleColor.DarkGray);
        var searchPath = Path.Combine(_workingDirectoryProvider.GetCurrentDirectory(), path);

        if (!Directory.Exists(searchPath))
        {
            return FormatXmlResponseForSearch("FAILED", path, regex, file_pattern, 0, 0, "Directory not found", null);
        }

        Regex compiledRegex;
        try
        {
            compiledRegex = new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException ex)
        {
            return FormatXmlResponseForSearch("FAILED", path, regex, file_pattern, 0, 0, $"Invalid regex pattern: {ex.Message}", null);
        }

        var filesToSearch = GetFilesMatchingPattern(searchPath, file_pattern);
        var matches = new ConcurrentBag<(string FilePath, int LineNumber, string Line)>();
        var filesSearchedCount = 0;

        try
        {
            Parallel.ForEach(filesToSearch, file =>
            {
                Interlocked.Increment(ref filesSearchedCount);
                try
                {
                    var lines = File.ReadLines(file);
                    var lineNumber = 0;
                    foreach (var line in lines)
                    {
                        lineNumber++;
                        if (compiledRegex.IsMatch(line))
                        {
                            matches.Add((file, lineNumber, line));
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore files that can't be read
                }
            });
        }
        catch (Exception ex)
        {
            return FormatXmlResponseForSearch("FAILED", path, regex, file_pattern, filesSearchedCount, 0, $"An error occurred during search: {ex.Message}", null);
        }

        var searchResults = new StringBuilder();
        foreach (var group in matches.GroupBy(m => m.FilePath).OrderBy(g => g.Key))
        {
            var relativePath = Path.GetRelativePath(searchPath, group.Key).Replace('\\', '/');
            searchResults.AppendLine($"<file path=\"{SecurityElement.Escape(relativePath)}\">");
            foreach (var match in group)
            {
                searchResults.AppendLine($"    <match line_number=\"{match.LineNumber}\" content=\"{SecurityElement.Escape(match.Line.Trim())}\" />");
            }
            searchResults.AppendLine("</file>");
        }

        return FormatXmlResponseForSearch("SUCCESS", path, regex, file_pattern, filesSearchedCount, matches.Count, null, searchResults.ToString());
    }

    private IEnumerable<string> GetFilesMatchingPattern(string rootPath, string? globPattern)
    {
        var matcher = new Matcher();
        if (!string.IsNullOrEmpty(globPattern))
        {
            matcher.AddInclude(globPattern);
        }
        else
        {
            matcher.AddInclude("**/*"); // Default to all files if no pattern is provided
        }

        // Exclude blacklisted directories for performance
        foreach (var blacklistedDir in BlacklistedDirectories)
        {
            matcher.AddExclude($"**/{blacklistedDir}/**");
        }

        return matcher.GetResultsInFullPath(rootPath);
    }
}
