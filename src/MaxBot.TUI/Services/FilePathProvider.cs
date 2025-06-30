namespace MaxBot.TUI.Services;

public class FilePathProvider(IWorkingDirectoryProvider workingDirectoryProvider, ILogger<FilePathProvider> logger) : IAutocompleteProvider
{
    private readonly IWorkingDirectoryProvider _workingDirectoryProvider = workingDirectoryProvider;
    private readonly ILogger<FilePathProvider> _logger = logger;

    public AutocompleteType Type => AutocompleteType.FilePath;
    public char TriggerCharacter => '@';

    public bool ShouldTrigger(string input, int cursorPosition)
    {
        _logger.LogDebug("FilePathProvider.ShouldTrigger called with input='{Input}', cursorPosition={CursorPosition}", input, cursorPosition);

        if (cursorPosition <= 0 || cursorPosition > input.Length)
        {
            _logger.LogDebug("FilePathProvider.ShouldTrigger returning false - invalid cursor position");
            return false;
        }

        // Look for @ character at or before cursor position
        for (var i = cursorPosition - 1; i >= 0; i--)
        {
            var ch = input[i];
            if (ch == '@')
            {
                // Found @, check if it's at word boundary or start
                if (i == 0 || char.IsWhiteSpace(input[i - 1]))
                {
                    _logger.LogDebug("FilePathProvider.ShouldTrigger returning true - found @ at valid position {Position}", i);
                    return true;
                }
            }
            else if (char.IsWhiteSpace(ch))
            {
                // Hit whitespace before finding @
                break;
            }
        }

        _logger.LogDebug("FilePathProvider.ShouldTrigger returning false - no @ found");
        return false;
    }

    public string ExtractPartial(string input, int cursorPosition)
    {
        if (cursorPosition <= 0 || cursorPosition > input.Length)
        {
            return string.Empty;
        }

        // Find the @ character
        var atIndex = -1;
        for (var i = cursorPosition - 1; i >= 0; i--)
        {
            var ch = input[i];
            if (ch == '@')
            {
                atIndex = i;
                break;
            }
            else if (char.IsWhiteSpace(ch))
            {
                break;
            }
        }

        if (atIndex == -1)
        {
            return string.Empty;
        }

        // Extract the path part after @
        var pathStart = atIndex + 1;
        if (pathStart >= input.Length)
        {
            return string.Empty;
        }

        var pathEnd = cursorPosition;
        return input[pathStart..pathEnd];
    }

    public async Task<List<CompletionItem>> GetSuggestionsAsync(string partialInput)
    {
        _logger.LogDebug("FilePathProvider.GetSuggestionsAsync called with partialInput='{PartialInput}'", partialInput);

        try
        {
            var workingDir = _workingDirectoryProvider.GetCurrentDirectory();
            _logger.LogDebug("Working directory: {WorkingDir}", workingDir);

            var searchPath = string.IsNullOrEmpty(partialInput) ? workingDir : Path.Combine(workingDir, partialInput);
            _logger.LogDebug("Search path: {SearchPath}", searchPath);

            // If partialInput ends with a path separator, search in that directory
            string searchDirectory;
            string searchPattern;

            if (partialInput.EndsWith('/') || partialInput.EndsWith('\\'))
            {
                searchDirectory = Path.GetFullPath(searchPath);
                searchPattern = string.Empty;
            }
            else if (string.IsNullOrEmpty(partialInput))
            {
                // When partialInput is empty, search in the working directory
                searchDirectory = workingDir;
                searchPattern = string.Empty;
            }
            else
            {
                searchDirectory = Path.GetDirectoryName(searchPath) ?? workingDir;
                searchPattern = Path.GetFileName(partialInput);
            }

            _logger.LogDebug("Search directory: {SearchDirectory}, pattern: '{SearchPattern}'", searchDirectory, searchPattern);

            // Ensure we stay within the working directory for security
            if (!IsWithinWorkingDirectory(searchDirectory, workingDir))
            {
                _logger.LogDebug("Search directory is outside working directory, returning empty results");
                return [];
            }

            if (!Directory.Exists(searchDirectory))
            {
                _logger.LogDebug("Search directory does not exist, returning empty results");
                return [];
            }

            var completionItems = new List<CompletionItem>();

            // Get directories
            var directories = Directory.GetDirectories(searchDirectory)
                .Where(dir => string.IsNullOrEmpty(searchPattern) || 
                             Path.GetFileName(dir).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                .Take(20); // Limit results

            _logger.LogDebug("Found {DirectoryCount} directories", directories.Count());

            foreach (var dir in directories)
            {
                var relativePath = GetRelativePath(dir, workingDir);
                var displayName = relativePath + "/";
                completionItems.Add(new CompletionItem(displayName, "Directory", CompletionItemType.Directory));
                _logger.LogDebug("Added directory: {DisplayName}", displayName);
            }

            // Get files
            var files = Directory.GetFiles(searchDirectory)
                .Where(file => string.IsNullOrEmpty(searchPattern) || 
                              Path.GetFileName(file).StartsWith(searchPattern, StringComparison.OrdinalIgnoreCase))
                .Take(20); // Limit results

            _logger.LogDebug("Found {FileCount} files", files.Count());

            foreach (var file in files)
            {
                var relativePath = GetRelativePath(file, workingDir);
                var fileInfo = new FileInfo(file);
                var description = $"File ({fileInfo.Length} bytes)";
                completionItems.Add(new CompletionItem(relativePath, description, CompletionItemType.File));
                _logger.LogDebug("Added file: {RelativePath}", relativePath);
            }

            _logger.LogDebug("Returning {CompletionCount} completion items", completionItems.Count);
            return await Task.FromResult(completionItems.OrderBy(c => c.Type).ThenBy(c => c.Text).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file path suggestions for partial: {Partial}", partialInput);
            return [];
        }
    }

    public (string newInput, int newCursorPos) ReplacePartial(string input, int cursorPos, string completion)
    {
        if (cursorPos <= 0 || cursorPos > input.Length)
        {
            return (input, cursorPos);
        }

        // Find the @ character
        var atIndex = -1;
        for (var i = cursorPos - 1; i >= 0; i--)
        {
            var ch = input[i];
            if (ch == '@')
            {
                atIndex = i;
                break;
            }
            else if (char.IsWhiteSpace(ch))
            {
                break;
            }
        }

        if (atIndex == -1)
        {
            return (input, cursorPos);
        }

        // Replace from after @ to cursor position
        var beforeAt = input[..(atIndex + 1)];
        var afterCursor = input[cursorPos..];
        var newInput = beforeAt + completion + afterCursor;
        var newCursorPos = atIndex + 1 + completion.Length;

        return (newInput, newCursorPos);
    }

    private bool IsWithinWorkingDirectory(string path, string workingDir)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var fullWorkingDir = Path.GetFullPath(workingDir);
            return fullPath.StartsWith(fullWorkingDir, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string GetRelativePath(string fullPath, string workingDir)
    {
        try
        {
            var relativePath = Path.GetRelativePath(workingDir, fullPath);
            return relativePath.Replace('\\', '/'); // Normalize to forward slashes
        }
        catch
        {
            return Path.GetFileName(fullPath);
        }
    }
}
